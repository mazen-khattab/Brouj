using System.Data.Common;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.IntegrationTests.Services;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Verifies the existing raw SQL boundaries against real SQL Server when explicitly enabled.</summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class FoundationSecurityTests(SqlServerFixture fixture)
{
    [SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actor_lock_SQL_uses_GUID_parameters_and_rejects_injection_credentials(bool user)
    {
        const string injection = "'; DROP TABLE [Customers]; DROP TABLE [Users]; --";
        await using var database = await fixture.CreateDatabaseAsync();
        Guid actorId;

        await using (var context = database.CreateContext())
        {
            var graph = await TestGraph.SeedAsync(context, reservations: false, order: false);
            graph.Customer.FName = injection;
            graph.User.Name = injection;
            await context.SaveChangesAsync();
            actorId = user ? graph.User.Id : graph.Customer.Id;
        }

        var commands = new CapturingCommandInterceptor();
        var clock = new FixedClock(AuthenticationTests.Now);
        var service = new RefreshTokenService(
            new CapturingFactory(database, clock, commands),
            AuthenticationTests.Service(clock: clock),
            clock);
        var session = user
            ? await service.CreateUserAsync(actorId, CancellationToken.None)
            : await service.CreateCustomerAsync(actorId, CancellationToken.None);

        Assert.NotNull(session);
        var actorLock = Assert.Single(commands.ActorLocks);
        Assert.Contains(user ? "[Users] WITH" : "[Customers] WITH", actorLock.Sql);
        Assert.DoesNotContain(actorId.ToString("D"), actorLock.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(injection, actorLock.Sql);
        Assert.Equal(actorId, Assert.IsType<Guid>(Assert.Single(actorLock.Parameters)));
        var previousCommandCount = commands.ReaderCount;

        Assert.Null(user
            ? await service.RotateUserAsync(injection, CancellationToken.None)
            : await service.RotateCustomerAsync(injection, CancellationToken.None));
        Assert.False(user
            ? await service.RevokeUserAsync(injection, CancellationToken.None)
            : await service.RevokeCustomerAsync(injection, CancellationToken.None));
        Assert.Equal(previousCommandCount, commands.ReaderCount);

        // The adversarial text remains data, and rejecting it as a credential leaves the session usable.
        Assert.NotNull(user
            ? await service.RotateUserAsync(session.RefreshToken, CancellationToken.None)
            : await service.RotateCustomerAsync(session.RefreshToken, CancellationToken.None));
        await using var verification = database.CreateContext();
        Assert.Equal(injection, await verification.Customers.AsNoTracking().Select(actor => actor.FName).SingleAsync());
        Assert.Equal(injection, await verification.Users.AsNoTracking().Select(actor => actor.Name).SingleAsync());
    }

    private sealed class CapturingFactory(
        TestDatabase database,
        TimeProvider clock,
        IInterceptor interceptor) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext()
        {
            return database.CreateContext(clock: clock, enableRetry: true, interceptors: [interceptor]);
        }
    }

    private sealed class CapturingCommandInterceptor : DbCommandInterceptor
    {
        public int ReaderCount { get; private set; }

        public List<(string Sql, object?[] Parameters)> ActorLocks { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCount++;

            if (command.CommandText.Contains("WITH (UPDLOCK, HOLDLOCK)", StringComparison.Ordinal))
            {
                ActorLocks.Add((
                    command.CommandText,
                    command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).ToArray()));
            }

            return ValueTask.FromResult(result);
        }
    }
}
