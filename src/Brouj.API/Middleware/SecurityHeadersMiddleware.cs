namespace Brouj.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var path = context.Request.Path;
            var isDevelopmentSwagger = environment.IsDevelopment()
                && (path == "/" || path == "/index.html" || path.StartsWithSegments("/swagger"));

            // Only the development documentation UI may load scripts, styles and same-origin API data.
            // API responses and all production responses retain the restrictive policy.
            context.Response.Headers.ContentSecurityPolicy = isDevelopmentSwagger
                ? "default-src 'none'; "
                    + "script-src 'self' 'unsafe-inline'; "
                    + "style-src 'self' 'unsafe-inline'; "
                    + "img-src 'self' data:; "
                    + "font-src 'self'; "
                    + "connect-src 'self'; "
                    + "frame-ancestors 'none'; "
                    + "base-uri 'self'"
                : "default-src 'none'; frame-ancestors 'none'";

            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // Responses may contain private DTOs or Set-Cookie; caching requires a separately reviewed opt-in.
            context.Response.Headers.CacheControl = "no-store";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
