using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Infrastructure.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task Factory_contexts_are_independent_of_the_scoped_application_context()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Server=localhost;Database=ModelOnly;Integrated Security=True;");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var first = await factory.CreateDbContextAsync(CancellationToken.None);
        await using var second = await factory.CreateDbContextAsync(CancellationToken.None);

        // Independent refresh operations own factory contexts. They must not share tracked state
        // with each other or replace the request's effective unit of work.
        first.Cities.Add(new Brouj.Domain.Entities.City { Name = "synthetic-tracked-city" });

        Assert.NotSame(first, second);
        Assert.NotSame(scoped, first);
        Assert.Same(scoped, scope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
        Assert.Single(first.ChangeTracker.Entries());
        Assert.Empty(second.ChangeTracker.Entries());
        Assert.Empty(scoped.ChangeTracker.Entries());
    }

    [Fact]
    public void Persistence_contract_and_concrete_context_share_one_scoped_instance()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser>(_ => new TestActor());
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        services.AddSingleton<TimeProvider>(clock);
        services.AddInfrastructure("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True;");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Same(context, scope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
        Assert.Same(clock, scope.ServiceProvider.GetRequiredService<TimeProvider>());
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(26, context.Model.GetEntityTypes().Count());
        using var otherScope = provider.CreateScope();
        Assert.NotSame(context, otherScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
