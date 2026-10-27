using Brouj.Application;
using Brouj.Infrastructure;
using Brouj.API.Configuration;
using Brouj.API.Extensions;
using Brouj.API.Middleware;
using Microsoft.Extensions.Options;
using Brouj.Infrastructure.Seeder;

namespace Brouj.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Infrastructure binds and validates JWT, cookie, caching, and storage settings.
            // Signing keys and connection credentials come from configuration/secret providers.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(connectionString);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddBroujCookieAuthentication();
            builder.Services.AddApiServices(builder.Configuration);

            var app = builder.Build();

            // Provisioning is opt-in through private configuration and finishes before accepting traffic.
            // A failure is sanitized by the seeder and deliberately stops startup.
            await using (var scope = app.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<UsersSeeding>()
                    .SeedAsync(app.Lifetime.ApplicationStopping);
            }

            // Forward only scheme/client IP from explicitly configured proxies, before HTTPS or rate limiting.
            app.UseForwardedHeaders();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseStatusCodePages(context => SafeProblems.WriteAsync(context.HttpContext,
                context.HttpContext.Response.StatusCode));
            app.Use(async (context, next) =>
            {
                var api = context.RequestServices.GetRequiredService<IOptions<ApiOptions>>().Value;

                // Validate before redirecting so untrusted Host headers cannot select a redirect destination.
                if (!string.Equals(context.Request.Host.Host, new Uri(api.BaseUrl).Host,
                    StringComparison.OrdinalIgnoreCase))
                {
                    await SafeProblems.WriteAsync(context, 400);
                    return;
                }

                await next(context);
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseRateLimiter();
            app.UseMiddleware<RequestLimitsMiddleware>();

            // CSRF validation needs the authenticated identity and must run before authorization.
            // Keep these calls adjacent so unsafe requests cannot bypass cookie protection.
            app.UseAuthentication();

            if (!app.Environment.IsDevelopment())
            {
                app.UseBroujCookieProtection();
            }

            app.UseAuthorization();

            app.MapControllers();

            // OpenAPI is a development aid. Production never exposes its schema or a Swagger UI.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi().RequireRateLimiting(RateLimitCategories.PublicReads);
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Brouj API V1");
                    options.RoutePrefix = string.Empty;
                });

                app.Logger.LogInformation("Development API documentation is available at /index.html.");
            }

            app.Run();
        }
    }
}
