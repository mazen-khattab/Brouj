using Brouj.API.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.API.Extensions;

/// <summary>Preserves a safe HTTP conflict marker before Application sanitizes persistence failures.</summary>
public sealed class ConcurrencyExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // This maps existing optimistic conflicts; it adds no locking or retry business behavior.
            throw new ApiConcurrencyException();
        }
    }
}
