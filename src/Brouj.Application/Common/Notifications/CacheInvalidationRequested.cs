using MediatR;
namespace Brouj.Application.Common.Notifications;


public sealed class CacheInvalidationRequested : INotification
{
    public CacheInvalidationRequested(string resource)
    {
        Resource = resource;
    }

    public string Resource { get; }
}
