using Brouj.Domain.Enums;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.IntegrationTests.Services;
using Brouj.Infrastructure.Persistence;
using Brouj.Infrastructure.Seeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[Collection(SqlServerCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class Part10BootstrapTests(SqlServerFixture fixture)
{
    [SqlServerFact]
    public async Task All_admin_fields_come_from_private_configuration_and_existing_accounts_are_preserved()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var options = InitialAdminOptionsTests.ValidOptions();
        options.Name = " Configured administrator ";
        options.Email = " Configured.Admin@Example.Test ";

        await using (var context = database.CreateContext(enableRetry: true))
        {
            await Seeder(context, options).SeedAsync(CancellationToken.None);
        }

        await using var reader = database.CreateContext();
        var admin = await reader.Users.AsNoTracking().SingleAsync();

        Assert.Equal(options.Id, admin.Id);
        Assert.Equal("Configured administrator", admin.Name);
        Assert.Equal("configured.admin@example.test", admin.Email);
        Assert.Equal(options.Phone, admin.Phone);
        Assert.Equal(UserRole.SuperAdmin, admin.Role);
        Assert.NotEqual(options.Password, admin.HashPassword);
        Assert.True(new PasswordHasher().Verify(options.Password, admin.HashPassword));

        var originalHash = admin.HashPassword;
        options.Password = "A-different-synthetic-password!";

        await using (var restarted = database.CreateContext(enableRetry: true))
        {
            await Seeder(restarted, options).SeedAsync(CancellationToken.None);
        }

        Assert.Equal(1, await reader.Users.CountAsync());
        Assert.Equal(originalHash, await reader.Users.Select(user => user.HashPassword).SingleAsync());
    }

    [SqlServerFact]
    public async Task Concurrent_startup_creates_exactly_one_initial_administrator()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        var options = InitialAdminOptionsTests.ValidOptions();
        var rendezvous = new Part9RequestRendezvous();

        async Task StartAsync()
        {
            await using var context = database.CreateContext(enableRetry: true, interceptors: [rendezvous]);

            await Seeder(context, options).SeedAsync(CancellationToken.None);
        }

        await Task.WhenAll(StartAsync(), StartAsync());

        Assert.Equal(2, rendezvous.Arrivals);

        await using var reader = database.CreateContext();

        Assert.Equal(options.Id, await reader.Users.Select(user => user.Id).SingleAsync());
    }

    [SqlServerFact]
    public async Task Bootstrap_failure_stops_startup_without_leaking_configuration_or_provider_details()
    {
        await using var database = await fixture.CreateDatabaseAsync();
        using var logs = new SecurityLogProvider();
        using var logging = LoggerFactory.Create(builder => builder.AddProvider(logs));
        await using var context = database.CreateContext(enableRetry: true, interceptors: [new RejectBootstrapSave()]);
        var options = InitialAdminOptionsTests.ValidOptions();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Seeder(context, options, logging.CreateLogger<UsersSeeding>()).SeedAsync(CancellationToken.None));

        Assert.Equal("Initial administrator provisioning failed.", error.Message);
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(logs.Entries, entry => entry.Contains(options.Password, StringComparison.Ordinal) ||
            entry.Contains(options.Email, StringComparison.Ordinal) || entry.Contains("PRIVATE-SQL-MARKER", StringComparison.Ordinal));

        await using var reader = database.CreateContext();

        Assert.False(await reader.Users.AnyAsync());
    }

    private static UsersSeeding Seeder(
        ApplicationDbContext context, InitialAdminOptions options, ILogger<UsersSeeding>? logger = null)
    {
        return new UsersSeeding(context,
            new ApplicationTransactionManager(context, NullLogger<ApplicationTransactionManager>.Instance),
            new PasswordHasher(), Options.Create(options), logger ?? NullLogger<UsersSeeding>.Instance);
    }

    private sealed class RejectBootstrapSave : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("PRIVATE-SQL-MARKER");
        }
    }
}
