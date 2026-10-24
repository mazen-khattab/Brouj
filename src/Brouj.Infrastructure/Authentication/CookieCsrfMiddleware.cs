using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Validates CSRF for unsafe browser requests, including requests with only a refresh cookie.</summary>
/// <remarks>Place after authentication and before authorization/endpoints. Enable for the entire browser
/// authentication pipeline so a forged login cannot replace the victim's session (login CSRF).</remarks>
public sealed class CookieCsrfMiddleware(RequestDelegate next)
{
    /// <summary>Rejects an unsafe request before it can reach a handler unless framework antiforgery validation succeeds.</summary>
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
        }
        await next(context);
    }
}
