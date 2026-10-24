using System.Data.Common;
using Brouj.Domain.Entities;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class PersistenceLoggingSecurityTests
{
    private const string PrivateValue = "synthetic-password-hash-token-identity\r\nforged-provider-log";

    [Fact]
    public async Task Retried_operations_do_not_log_the_original_exception()
    {
        using var logs = new SecurityLogProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs));
        services.AddInfrastructure("Server=localhost;Database=SecurityDiagnosticsOnly;Integrated Security=True;");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var attempts = 0;

        // Exercise the real SQL Server retry strategy without connecting to a server. The first
        // retry has no delay, and the second attempt succeeds without doing any database work.
        var result = await context.Database.CreateExecutionStrategy().ExecuteAsync(token =>
        {
            attempts++;

            if (attempts == 1)
            {
                throw new TimeoutException(PrivateValue);
            }

            return Task.FromResult(42);
        }, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(2, attempts);
        Assert.NotEmpty(logs.Entries);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("synthetic-password-hash-token-identity", entry);
            Assert.DoesNotContain("forged-provider-log", entry);
        });
    }

    [Theory]
    [InlineData("query")]
    [InlineData("update")]
    [InlineData("save")]
    public async Task Persistence_failures_do_not_log_provider_exception_details(string operation)
    {
        using var logs = new SecurityLogProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(logs));
        services.AddInfrastructure("Server=localhost;Database=SecurityDiagnosticsOnly;Integrated Security=True;");
        services.ConfigureDbContext<ApplicationDbContext>(options =>
            options.AddInterceptors(new FailingConnectionInterceptor()));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Fail before opening a physical connection. These exercise EF's real logging paths;
        // they make no claim about SQL execution, constraints, transactions or locking.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            switch (operation)
            {
                case "query":
                    await context.Customers.AsNoTracking().ToListAsync();
                    break;
                case "update":
                    await context.CustomerRefreshTokens.ExecuteUpdateAsync(set =>
                        set.SetProperty(token => token.IsActive, false));
                    break;
                default:
                    context.Cities.Add(new City { Name = "synthetic-city" });
                    await context.SaveChangesAsync();
                    break;
            }
        });

        Assert.Equal(PrivateValue, error.Message);
        Assert.NotEmpty(logs.Entries);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain("synthetic-password-hash-token-identity", entry);
            Assert.DoesNotContain("forged-provider-log", entry);
        });
    }

    private sealed class FailingConnectionInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection,
            ConnectionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(PrivateValue);
        }
    }
}
