using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Notifications;
using Brouj.Application.Common.Services;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Brouj.Application.Behaviors;


public sealed class CommittedEffectsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly PendingFileCleanup _cleanup;
    private readonly IPublisher _publisher;
    private readonly ILogger<CommittedEffectsBehavior<TRequest, TResponse>> _logger;

    public CommittedEffectsBehavior(
        PendingFileCleanup cleanup,
        IPublisher publisher,
        ILogger<CommittedEffectsBehavior<TRequest, TResponse>> logger)
    {
        _cleanup = cleanup;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        TResponse response;

        try
        {
            // This behavior surrounds TransactionBehavior, so effects run only after commit.
            response = await next(cancellationToken);
        }
        catch
        {
            _cleanup.Discard();

            throw;
        }

        if (response is not Result { IsSuccess: true })
        {
            _cleanup.Discard();

            return response;
        }

        await _cleanup.CompleteAsync();

        var intent = Attribute.GetCustomAttribute(typeof(TRequest), typeof(InvalidatesCacheAttribute))
            as InvalidatesCacheAttribute;

        if (intent is not null)
        {
            foreach (var resource in intent.Resources)
            {
                try
                {
                    await _publisher.Publish(new CacheInvalidationRequested(resource), CancellationToken.None);
                }
                catch
                {
                    _logger.LogWarning("A committed write's cache invalidation intent could not be delivered.");
                }
            }
        }

        return response;
    }
}
