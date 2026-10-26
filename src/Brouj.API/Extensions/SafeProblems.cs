using Microsoft.AspNetCore.Mvc;

namespace Brouj.API.Extensions;

/// <summary>Transport failures contain fixed messages, never exception or model-binding diagnostics.</summary>
public static class SafeProblems
{
    public static ProblemDetails Create(HttpContext context, int status)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status switch
            {
                400 => "The request is invalid.",
                401 => "Authentication is required.",
                403 => "Access is denied.",
                404 => "The resource was not found.",
                409 => "The operation conflicts with the current resource state.",
                413 => "The request payload is too large.",
                415 => "The content type is not supported.",
                429 => "Too many requests.",
                _ => "The request could not be completed."
            }
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return problem;
    }

    public static Task WriteAsync(HttpContext context, int status)
    {
        context.Response.StatusCode = status;

        return context.Response.WriteAsJsonAsync(Create(context, status),
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
