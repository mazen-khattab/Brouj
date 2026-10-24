using System.Data;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Provides one active session per actor using existing unique indexes and SQL Server row locks.</summary>
/// <remarks>No process-local lock participates in correctness. The factory isolates authentication writes
/// from unrelated tracked application entities, and creates a clean context on every transient retry.</remarks>
public sealed class RefreshTokenService(IDbContextFactory<ApplicationDbContext> factory, IJwtTokenService jwt, TimeProvider clock) : IRefreshTokenService
{
    public Task<RefreshTokenResult?> CreateCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        IssueAsync(customerId, false, null, cancellationToken);

    public Task<RefreshTokenResult?> CreateUserAsync(Guid userId, CancellationToken cancellationToken) =>
        IssueAsync(userId, true, null, cancellationToken);

    public Task<RefreshTokenResult?> RotateCustomerAsync(string token, CancellationToken cancellationToken) =>
        RotateAsync(token, false, cancellationToken);

    public Task<RefreshTokenResult?> RotateUserAsync(string token, CancellationToken cancellationToken) =>
        RotateAsync(token, true, cancellationToken);

    public Task<bool> RevokeCustomerAsync(string token, CancellationToken cancellationToken) => RevokeAsync(token, false, cancellationToken);

    public Task<bool> RevokeUserAsync(string token, CancellationToken cancellationToken) => RevokeAsync(token, true, cancellationToken);

    private Task<RefreshTokenResult?> RotateAsync(string token, bool user, CancellationToken ct) =>
        RefreshTokenDigest.TryHash(token, out var hash) ? IssueAsync(Guid.Empty, user, hash, ct) : Task.FromResult<RefreshTokenResult?>(null);

    private async Task<RefreshTokenResult?> IssueAsync(Guid actorId, bool user, string? oldHash, CancellationToken ct)
    {
        if (oldHash is null && actorId == Guid.Empty)
        {
            return null;
        }

        var rawToken = jwt.GenerateRefreshToken();

        if (!RefreshTokenDigest.TryHash(rawToken, out var newHash))
        {
            throw new InvalidOperationException("Invalid refresh generator.");
        }

        var newId = Guid.NewGuid();

        // Stable IDs and credentials let a retry recognize an uncertain-but-successful commit.
        // A retry must never repeat a login or rotate a different credential after that commit.
        return await ExecuteAsync(async (db, token) =>
        {
            var old = oldHash is null ? null : await FindAsync(db, user, oldHash, token);

            if (oldHash is not null && old is null)
            {
                return null;
            }

            var owner = old?.ActorId ?? actorId;

            var actor = await LockActorAsync(db, user, owner, token);

            if (actor is null || actor.Deleted || (user && !Enum.IsDefined(actor.Role)))
            {
                return null;
            }

            var previousAttempt = await FindAsync(db, user, newHash, token);

            if (previousAttempt is not null)
            {
                return previousAttempt.Id == newId &&
                previousAttempt.ActorId == owner && 
                previousAttempt.Active && 
                previousAttempt.Expires > Now() ? Result(user, actor, owner, rawToken, previousAttempt.Expires) : null;
            }


            var now = Now();
            DateTimeOffset expires;

            if (old is not null)
            {
                // Recheck through a conditional UPDATE after acquiring the actor lock. Exactly one
                // request can consume the credential; losers leave the replacement session alone.
                if (!old.Active)
                {
                    return null;
                }
                if (old.Expires <= now)
                {
                    await DeactivateAsync(db, user, old.Id, oldHash!, false, now, token);
                    return null;
                }
                if (await DeactivateAsync(db, user, old.Id, oldHash!, true, now, token) != 1)
                {
                    return null;
                }
                expires = old.Expires;
            }
            else
            {
                var revoked = await RevokeActorAsync(db, user, owner, token);
                if (revoked > 1)
                {
                    throw new InvalidOperationException("Active refresh-session invariant violated.");
                }
                expires = now.AddDays(7);
            }

            if (user)
            {
                db.UserRefreshTokens.Add(new UserRefreshToken
                {
                    Id = newId,
                    UserId = owner,
                    Token = newHash,
                    IsActive = true,
                    CreatedAt = now,
                    ExpDate = expires
                });
            }
            else
            {
                db.CustomerRefreshTokens.Add(new CustomerRefreshToken
                {
                    Id = newId,
                    CustomerId = owner,
                    Token = newHash,
                    IsActive = true,
                    CreatedAt = now,
                    ExpDate = expires
                });
            }
                
            await db.SaveChangesAsync(token);
            return Result(user, actor, owner, rawToken, expires);
        }, ct);
    }

    private async Task<bool> RevokeAsync(string token, bool user, CancellationToken ct)
    {
        if (!RefreshTokenDigest.TryHash(token, out var hash))
        {
            return false;
        }

        return await ExecuteAsync(async (db, cancellation) =>
        {
            var row = await FindAsync(db, user, hash, cancellation);

            if (row is null || !row.Active)
            {
                return false;
            }

            if (await LockActorAsync(db, user, row.ActorId, cancellation) is null)
            {
                return false;
            }

            return await DeactivateAsync(db, user, row.Id, hash, false, Now(), cancellation) == 1;
        }, ct);
    }

    private async Task<T?> ExecuteAsync<T>(Func<ApplicationDbContext, CancellationToken, Task<T?>> operation, CancellationToken ct)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        try
        {
            return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async cancellation =>
            {
                await using var db = await factory.CreateDbContextAsync(cancellation);
                await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellation);

                var result = await operation(db, cancellation);
                await transaction.CommitAsync(cancellation);
                return result;
            }, ct);
        }
        catch (DbUpdateConcurrencyException) 
        {
            return default;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Disposal rolls back consumption/revocation as well as insertion. Never turn a unique
            // constraint race into a partially committed session or disclose conflicting token digests.
            return default;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqlException or Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException)
        {
            // SQL diagnostics can contain stored credential digests; keep them out of the exception chain.
            throw new InvalidOperationException("Refresh-session persistence failed.");
        }
    }

    private RefreshTokenResult Result(bool user, Actor actor, Guid owner, string raw, DateTimeOffset expires) => new(
        user ? jwt.GenerateUserAccessToken(owner, actor.Role) : jwt.GenerateCustomerAccessToken(owner), raw, expires);

    private DateTimeOffset Now() => DateTimeOffset.FromUnixTimeMilliseconds(clock.GetUtcNow().ToUnixTimeMilliseconds());

    private static Task<Session?> FindAsync(ApplicationDbContext db, bool user, string hash, CancellationToken ct) => user
        ? db.UserRefreshTokens.AsNoTracking().Where(t => t.Token == hash)
            .Select(t => new Session(t.Id, t.UserId, t.IsActive, t.ExpDate)).SingleOrDefaultAsync(ct)
        : db.CustomerRefreshTokens.AsNoTracking().Where(t => t.Token == hash)
            .Select(t => new Session(t.Id, t.CustomerId, t.IsActive, t.ExpDate)).SingleOrDefaultAsync(ct);

    private static Task<Actor?> LockActorAsync(ApplicationDbContext db, bool user, Guid id, CancellationToken ct) => user
        ? db.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking().Select(a => new Actor(a.DeletedAt != null, a.Role)).SingleOrDefaultAsync(ct)
        : db.Customers.FromSqlInterpolated($"SELECT * FROM [Customers] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}")
            .AsNoTracking().Select(a => new Actor(a.DeletedAt != null, UserRole.Admin)).SingleOrDefaultAsync(ct);

    private static Task<int> DeactivateAsync(ApplicationDbContext db, bool user, Guid id, string hash,
        bool requireUnexpired, DateTimeOffset now, CancellationToken ct) => user
        ? db.UserRefreshTokens.Where(t => t.Id == id && t.Token == hash && t.IsActive && (!requireUnexpired || t.ExpDate > now))
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.IsActive, false), ct)
        : db.CustomerRefreshTokens.Where(t => t.Id == id && t.Token == hash && t.IsActive && (!requireUnexpired || t.ExpDate > now))
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.IsActive, false), ct);

    private static Task<int> RevokeActorAsync(ApplicationDbContext db, bool user, Guid id, CancellationToken ct) => user
        ? db.UserRefreshTokens.Where(t => t.UserId == id && t.IsActive).ExecuteUpdateAsync(set => set.SetProperty(t => t.IsActive, false), ct)
        : db.CustomerRefreshTokens.Where(t => t.CustomerId == id && t.IsActive).ExecuteUpdateAsync(set => set.SetProperty(t => t.IsActive, false), ct);

    private sealed record Session(Guid Id, Guid ActorId, bool Active, DateTimeOffset Expires);
    private sealed record Actor(bool Deleted, UserRole Role);
}
