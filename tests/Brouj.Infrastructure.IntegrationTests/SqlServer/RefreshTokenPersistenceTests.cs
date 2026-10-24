using Brouj.Application.Abstractions.Authentication;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.IntegrationTests.Services;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Security.Claims;
using Brouj.Domain.Enums;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Real SQL Server lifecycle/locking verification; stays explicitly pending without a test server/runtime.</summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class RefreshTokenPersistenceTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rotation_preserves_deadline_stores_only_hash_and_replay_cannot_revoke_the_winner(bool user)
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, staff) = await SeedAsync(db);
        var service = Service(db);
        var login = (await CreateAsync(service, user, user ? staff : customer))!;
        var rotated = (await RotateAsync(service, user, login.RefreshToken))!;
        Assert.Equal(login.ExpiresAt, rotated.ExpiresAt);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.Null(await RotateAsync(service, user, login.RefreshToken));
        Assert.False(await RevokeAsync(service, user, login.RefreshToken));
        await using var context = db.CreateContext();
        var hashes = user ? await context.UserRefreshTokens.Select(t => t.Token).ToListAsync()
            : await context.CustomerRefreshTokens.Select(t => t.Token).ToListAsync();
        Assert.DoesNotContain(login.RefreshToken, hashes);
        Assert.DoesNotContain(rotated.RefreshToken, hashes);
        Assert.True(RefreshTokenDigest.TryHash(rotated.RefreshToken, out var digest));
        Assert.Contains(digest, hashes);
        Assert.True(await RevokeAsync(service, user, rotated.RefreshToken));
        Assert.Null(await RotateAsync(service, user, rotated.RefreshToken));
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_refresh_has_one_winner_and_a_valid_replacement(bool user)
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, staff) = await SeedAsync(db);
        var login = (await CreateAsync(Service(db), user, user ? staff : customer))!;
        // Each caller owns an independent factory/context, representing separate application instances.
        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => RotateAsync(Service(db), user, login.RefreshToken)));
        var winner = Assert.Single(attempts, result => result is not null)!;
        Assert.NotNull(await RotateAsync(Service(db), user, winner.RefreshToken));
        await using var context = db.CreateContext();
        Assert.Equal(1, user ? await context.UserRefreshTokens.CountAsync(t => t.IsActive)
            : await context.CustomerRefreshTokens.CountAsync(t => t.IsActive));
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_logins_keep_only_one_session_and_expired_tokens_are_rejected(bool user)
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, staff) = await SeedAsync(db);
        var clock = new MutableClock(AuthenticationTests.Now);
        var service = Service(db, clock);
        var logins = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => CreateAsync(service, user, user ? staff : customer)));
        Assert.All(logins, login => Assert.NotNull(login));
        await using var context = db.CreateContext();
        var activeHash = user ? await context.UserRefreshTokens.Where(t => t.IsActive).Select(t => t.Token).SingleAsync()
            : await context.CustomerRefreshTokens.Where(t => t.IsActive).Select(t => t.Token).SingleAsync();
        var winner = Assert.Single(logins, login => RefreshTokenDigest.TryHash(login!.RefreshToken, out var hash) && hash == activeHash)!;
        foreach (var loser in logins.Where(login => login != winner)) Assert.Null(await RotateAsync(service, user, loser!.RefreshToken));
        clock.Advance(TimeSpan.FromDays(7));
        Assert.Null(await RotateAsync(service, user, winner.RefreshToken));
        Assert.Equal(0, user ? await context.UserRefreshTokens.CountAsync(t => t.IsActive)
            : await context.CustomerRefreshTokens.CountAsync(t => t.IsActive));
    }

    [SqlServerFact]
    public async Task Wrong_actor_table_and_soft_deleted_actors_cannot_refresh()
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, _) = await SeedAsync(db);
        var service = Service(db);
        var login = (await service.CreateCustomerAsync(customer, default))!;
        Assert.Null(await service.RotateUserAsync(login.RefreshToken, default));
        await using var context = db.CreateContext();
        await context.Customers.Where(c => c.Id == customer).ExecuteUpdateAsync(set => set.SetProperty(c => c.DeletedAt, DateTimeOffset.UtcNow));
        Assert.Null(await service.RotateCustomerAsync(login.RefreshToken, default));
        Assert.Null(await service.CreateCustomerAsync(customer, default));
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unique_token_failure_rolls_back_revocation_and_returns_a_generic_failure(bool user)
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, staff) = await SeedAsync(db);
        var normal = Service(db);
        var id = user ? staff : customer;
        var original = (await CreateAsync(normal, user, id))!;
        var clock = new FixedClock(AuthenticationTests.Now);
        var duplicateGenerator = new RepeatingTokenGenerator(AuthenticationTests.Service(clock: clock), original.RefreshToken);
        var colliding = new RefreshTokenService(new TestFactory(db, clock), duplicateGenerator, clock);
        Assert.Null(await CreateAsync(colliding, user, id));
        // Failed insertion must not commit the previous credential's revocation.
        Assert.NotNull(await RotateAsync(normal, user, original.RefreshToken));
    }

    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execution_strategy_recovers_an_uncertain_commit_without_creating_a_second_session(bool rotate)
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, _) = await SeedAsync(db);
        var clock = new FixedClock(AuthenticationTests.Now);
        var old = rotate ? (await Service(db).CreateCustomerAsync(customer, default))!.RefreshToken : null;
        var fault = new CommitAcknowledgementFailure();
        var retrying = new RefreshTokenService(new TestFactory(db, clock, fault), AuthenticationTests.Service(clock: clock), clock);
        var result = rotate ? await retrying.RotateCustomerAsync(old!, default) : await retrying.CreateCustomerAsync(customer, default);
        Assert.NotNull(result);
        Assert.True(fault.Thrown);
        await using var context = db.CreateContext();
        Assert.Equal(rotate ? 2 : 1, await context.CustomerRefreshTokens.CountAsync());
        Assert.Equal(1, await context.CustomerRefreshTokens.CountAsync(t => t.IsActive));
        Assert.NotNull(await Service(db).RotateCustomerAsync(result.RefreshToken, default));
    }

    [SqlServerFact]
    public async Task Logout_and_refresh_race_cannot_revoke_a_replacement_credential()
    {
        await using var db = await fixture.CreateDatabaseAsync();
        var (customer, _) = await SeedAsync(db);
        var original = (await Service(db).CreateCustomerAsync(customer, default))!;
        var rotation = Service(db).RotateCustomerAsync(original.RefreshToken, default);
        var logout = Service(db).RevokeCustomerAsync(original.RefreshToken, default);
        await Task.WhenAll(rotation, logout);
        var replacement = await rotation;
        if (replacement is not null)
        {
            Assert.False(await logout);
            Assert.NotNull(await Service(db).RotateCustomerAsync(replacement.RefreshToken, default));
        }
        else Assert.True(await logout);
    }

    [SqlServerFact]
    public async Task Application_transaction_rolls_back_failed_work_and_clears_stale_tracking()
    {
        await using var db = await fixture.CreateDatabaseAsync();
        await using var context = db.CreateContext(enableRetry: true);
        var manager = new ApplicationTransactionManager(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ExecuteAsync<int>(async ct =>
        {
            context.Cities.Add(new Brouj.Domain.Entities.City { Name = "rollback-test" });
            await context.SaveChangesAsync(ct);
            throw new InvalidOperationException("Synthetic operation failure.");
        }, default));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.False(await context.Cities.AnyAsync());
    }

    private static async Task<(Guid Customer, Guid User)> SeedAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var graph = await TestGraph.SeedAsync(context, reservations: false, order: false);
        // Synthetic Part 3 seed credentials are not production token data; remove them before lifecycle tests.
        await context.CustomerRefreshTokens.ExecuteDeleteAsync();
        await context.UserRefreshTokens.ExecuteDeleteAsync();
        return (graph.Customer.Id, graph.User.Id);
    }

    private static RefreshTokenService Service(TestDatabase database, TimeProvider? clock = null)
    {
        var time = clock ?? new FixedClock(AuthenticationTests.Now);
        return new RefreshTokenService(new TestFactory(database, time), AuthenticationTests.Service(clock: time), time);
    }
    private static Task<RefreshTokenResult?> CreateAsync(IRefreshTokenService service, bool user, Guid id) =>
        user ? service.CreateUserAsync(id, default) : service.CreateCustomerAsync(id, default);
    private static Task<RefreshTokenResult?> RotateAsync(IRefreshTokenService service, bool user, string raw) =>
        user ? service.RotateUserAsync(raw, default) : service.RotateCustomerAsync(raw, default);
    private static Task<bool> RevokeAsync(IRefreshTokenService service, bool user, string raw) =>
        user ? service.RevokeUserAsync(raw, default) : service.RevokeCustomerAsync(raw, default);

    private sealed class TestFactory(TestDatabase db, TimeProvider clock, IInterceptor? interceptor = null) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => db.CreateContext(clock: clock, enableRetry: true,
            interceptors: interceptor is null ? [] : [interceptor]);
    }

    // Failure happens AFTER the server committed, approximating a lost commit acknowledgement.
    // The retry must recognize its stable replacement ID instead of revoking and inserting again.
    private sealed class CommitAcknowledgementFailure : DbTransactionInterceptor
    {
        public bool Thrown { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!Thrown) { Thrown = true; throw new TimeoutException("Synthetic lost commit acknowledgement."); }
            return Task.CompletedTask;
        }
    }
    private sealed class RepeatingTokenGenerator(IJwtTokenService jwt, string repeated) : IJwtTokenService
    {
        public string GenerateRefreshToken() => repeated;
        public string GenerateCustomerAccessToken(Guid id) => jwt.GenerateCustomerAccessToken(id);
        public string GenerateUserAccessToken(Guid id, UserRole role) => jwt.GenerateUserAccessToken(id, role);
        public ClaimsPrincipal ValidateAccessToken(string token) => jwt.ValidateAccessToken(token);
    }
    private sealed class MutableClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
