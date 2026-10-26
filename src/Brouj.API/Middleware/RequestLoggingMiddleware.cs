using System.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace Brouj.API.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            // Route templates exclude user-controlled IDs, query strings, headers and request bodies.
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            _logger.LogInformation("HTTP route {Route} returned {StatusCode} in {ElapsedMilliseconds} ms; trace {TraceId}",
                route, context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                context.TraceIdentifier);
        }
    }
}
