using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Services;
using Brouj.Application.Common.Utils;
using MediatR;

namespace Brouj.Application.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly FeatureAccess _access;

    public AuthorizationBehavior(FeatureAccess access)
    {
        _access = access;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is not IRequireAccess protectedRequest)
        {
            // Public catalogue and session operations do not trigger an account permission query.
            return await next(cancellationToken);
        }

        var error = await _access.CheckAsync(protectedRequest.RequiredAccess, cancellationToken);

        if (error is not null)
        {
            return ResultFailureFactory<TResponse>.Create(error);
        }

        // In particular, do not start validation, transactions or handler side effects after cancellation.
        cancellationToken.ThrowIfCancellationRequested();

        return await next(cancellationToken);
    }
}
