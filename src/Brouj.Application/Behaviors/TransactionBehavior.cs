using Brouj.Application.Abstractions.Persistence;
using MediatR;

namespace Brouj.Application.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(IApplicationTransactionManager transactionManager)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest)
        {
            return next(cancellationToken);
        }

        return transactionManager.ExecuteAsync(operationCancellationToken => next(operationCancellationToken), cancellationToken);
    }
}
