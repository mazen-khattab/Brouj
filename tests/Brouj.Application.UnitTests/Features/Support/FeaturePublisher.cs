using MediatR;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeaturePublisher : IPublisher
{
    public List<object> Published { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        Published.Add(notification);

        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Publish((object)notification, cancellationToken);
}
