using Brouj.Application;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Abstractions.Storage;
using Brouj.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

/// <summary>Runs the registered MediatR pipeline, including transactions and deferred file cleanup.</summary>
internal sealed class Part9FeatureHost : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    public Part9FeatureHost(ApplicationDbContext context, ICurrentUser actor, IFileStorageService storage)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton<ICurrentUser>(actor);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(storage);
        services.AddSingleton<IApplicationTransactionManager>(provider => new ApplicationTransactionManager(
            context, provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ApplicationTransactionManager>>()));

        _services = services.BuildServiceProvider();
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        return _services.GetRequiredService<ISender>().Send(request);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
