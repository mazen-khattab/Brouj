using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Behaviors;
using Brouj.Application.Common.Services;
using Brouj.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brouj.Infrastructure.IntegrationTests.SqlServer;

internal static class ApplicationFeaturePipeline
{
    public static Task<TResponse> SendAsync<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        ApplicationDbContext context,
        ICurrentUser actor)
        where TRequest : IRequest<TResponse>
    {
        var authorization = new AuthorizationBehavior<TRequest, TResponse>(new FeatureAccess(context, actor));
        var validatorType = typeof(TRequest).Assembly.GetType(typeof(TRequest).FullName + "Validator")!;
        var validator = (IValidator<TRequest>)Activator.CreateInstance(validatorType)!;
        var validation = new ValidationBehavior<TRequest, TResponse>([validator]);
        var transaction = new TransactionBehavior<TRequest, TResponse>(
            new ApplicationTransactionManager(context, NullLogger<ApplicationTransactionManager>.Instance));

        // Exercise the same pre-handler boundary while retaining provider errors for relational assertions.
        return authorization.Handle(request, token =>
            validation.Handle(request, validationToken =>
                transaction.Handle(request, handlerToken => handler.Handle(request, handlerToken), validationToken), token),
            CancellationToken.None);
    }

    public static Task<TResponse> SendWithConflictMappingAsync<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler, TRequest request, ApplicationDbContext context, ICurrentUser actor)
        where TRequest : IRequest<TResponse>
    {
        var failures = new FeatureFailureBehavior<TRequest, TResponse>(
            NullLogger<FeatureFailureBehavior<TRequest, TResponse>>.Instance);

        // Preserve the normal outer mapping boundary while the existing helper can still assert raw SQL failures.
        return failures.Handle(request, _ => SendAsync(handler, request, context, actor), CancellationToken.None);
    }
}
