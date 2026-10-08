using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();

        logger.LogInformation("Handling application request {RequestType}", requestType);

        try
        {
            var response = await next(cancellationToken);

            logger.LogInformation("Completed application request {RequestType} in {ElapsedMilliseconds} ms", requestType, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Cancelled application request {RequestType} after {ElapsedMilliseconds} ms", requestType, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw;
        }
        catch
        {
            logger.LogWarning("Failed application request {RequestType} after {ElapsedMilliseconds} ms", requestType, Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

            throw;
        }
    }
}