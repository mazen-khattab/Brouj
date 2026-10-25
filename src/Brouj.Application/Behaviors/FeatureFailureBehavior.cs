using Brouj.Application.Common.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Brouj.Application.Behaviors;


public sealed class FeatureFailureBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or System.Data.Common.DbException)
        {
            throw new FeaturePersistenceException();
        }
    }
}
