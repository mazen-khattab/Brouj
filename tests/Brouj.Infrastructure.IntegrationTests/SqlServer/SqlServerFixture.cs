using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Interceptors;
using Brouj.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "Part 3 SQL Server";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        if (!SqlServerTestEnvironment.Enabled)
        {
            return;
        }

        _connectionString = Environment.GetEnvironmentVariable("BROUJ_SQLSERVER_TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Brouj-" + Guid.NewGuid().ToString("N") + "aA1!")
            .Build();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _container.StartAsync(timeout.Token);
        _connectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    internal async Task<TestDatabase> CreateDatabaseAsync()
    {
        var connection = new SqlConnectionStringBuilder(_connectionString
            ?? throw new InvalidOperationException(SqlServerTestEnvironment.PendingReason))
        {
            InitialCatalog = "BroujPart3_" + Guid.NewGuid().ToString("N"),
            MultipleActiveResultSets = false,
            TrustServerCertificate = true
        };
        var database = new TestDatabase(connection.ConnectionString);
        await using var context = database.CreateContext();
        await context.Database.EnsureCreatedAsync();
        return database;
    }
}

internal sealed class TestDatabase(string connectionString) : IAsyncDisposable
{
    public ApplicationDbContext CreateContext(TestActor? actor = null, TimeProvider? clock = null)
    {
        var timeProvider = clock ?? TimeProvider.System;
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options,
            new AuditableEntityInterceptor(timeProvider),
            new ActivityLogInterceptor(actor ?? new TestActor(), timeProvider));
    }

    public async ValueTask DisposeAsync()
    {
        // Only the fixture-generated BroujPart3_<GUID> database is dropped, never a supplied database.
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
