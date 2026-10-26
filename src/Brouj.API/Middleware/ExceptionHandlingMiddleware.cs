using Brouj.API.Extensions;
using FluentValidation;

namespace Brouj.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Do not turn a disconnected client into an unexpected application failure.
            context.Abort();
        }
        catch (Exception exception)
        {
            var status = exception switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                ApiConcurrencyException => StatusCodes.Status409Conflict,
                BadHttpRequestException bad => bad.StatusCode,
                // The storage adapter uses this fixed exception for rejected signatures/metadata.
                ArgumentException argument when argument.Message == "Invalid image upload." =>
                    StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            // Exception objects/messages can contain SQL values, credentials or local paths.
            _logger.LogWarning("API failure {FailureType} returned {StatusCode}; trace {TraceId}",
                exception.GetType().Name, status, context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                context.Abort();
                return;
            }

            context.Response.Clear();
            await SafeProblems.WriteAsync(context, status);
        }
    }
}
