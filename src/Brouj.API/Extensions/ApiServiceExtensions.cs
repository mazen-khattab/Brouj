using System.Net;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Brouj.API.Authorization;
using Brouj.API.Configuration;
using Brouj.API.Filters;
using Brouj.Application.Common.Settings;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Brouj.API.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        var api = configuration.GetSection("Api").Get<ApiOptions>() ?? new ApiOptions();
        var rates = configuration.GetSection("RateLimits").Get<RateLimitOptions>() ?? new RateLimitOptions();

        // Fail before binding a listener. Missing values must never silently disable protection.
        Validate(api, rates);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddOpenApi();

        // Freeze the validated snapshot so a configuration reload cannot bypass startup validation.
        services.AddSingleton(Options.Create(api));
        services.AddSingleton(Options.Create(rates));
        services.AddApiAuthorization();
        // Registered after Application behaviors so concurrency is sanitized before its provider-error catch.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyExceptionBehavior<,>));
        services.AddControllers(options => options.Filters.Add<UploadValidationFilter>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.MaxDepth = api.MaximumJsonDepth;
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            });
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var oversized = context.ModelState.Values.SelectMany(value => value.Errors)
                    .Any(error => error.Exception is BadHttpRequestException { StatusCode: 413 });
                var status = oversized ? 413 : 400;
                var result = new ObjectResult(SafeProblems.Create(context.HttpContext, status)) { StatusCode = status };
                result.ContentTypes.Add("application/problem+json");

                return result;
            };
        });
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = api.MultipartBodyLimitBytes);
        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = Math.Max(api.JsonBodyLimitBytes, api.MultipartBodyLimitBytes);
        });
        services.AddHttpsRedirection(options => options.HttpsPort = new Uri(api.BaseUrl).Port);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Empty trust lists plus enabled forwarding mean trust-all in ASP.NET Core. Disable instead.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.ForwardedHeaders = api.TrustedProxies.Length == 0 ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = api.ForwardLimit;

            foreach (var address in api.TrustedProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(address));
            }
        });
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(api.AllowedOrigins).AllowCredentials()
            .WithMethods("GET", "POST", "PATCH", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "X-CSRF-TOKEN", "Authorization")
            .WithExposedHeaders("Retry-After")));
        services.AddRateLimiter(options =>
        {
            AddRatePolicy(options, RateLimitCategories.PublicReads, rates.PublicReads);
            AddRatePolicy(options, RateLimitCategories.Authentication, rates.Authentication);
            AddRatePolicy(options, RateLimitCategories.ReservationWrites, rates.ReservationWrites);
            AddRatePolicy(options, RateLimitCategories.AdminWrites, rates.AdminWrites);
            AddRatePolicy(options, RateLimitCategories.FileUploads, rates.FileUploads);
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (rejection, cancellationToken) =>
            {
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    rejection.HttpContext.Response.Headers.RetryAfter =
                        Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                var logger = rejection.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Brouj.API.RateLimiting");
                logger.LogWarning("API rate limit rejected a request; trace {TraceId}",
                    rejection.HttpContext.TraceIdentifier);
                await SafeProblems.WriteAsync(rejection.HttpContext, 429);
            };
        });

        return services;
    }

    private static void AddRatePolicy(RateLimiterOptions options, string name, RateLimitPolicyOptions policy)
    {
        // Use the server-observed address after trusted forwarding; arbitrary headers never select a bucket.
        // IP partitions also limit anonymous login and failed authentication before JWT work begins.
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    }

    private static void Validate(ApiOptions api, RateLimitOptions rates)
    {
        if (!IsHttpsOrigin(api.BaseUrl) || api.AllowedOrigins.Length == 0 ||
            api.AllowedOrigins.Any(origin => !IsHttpsOrigin(origin)) ||
            api.TrustedProxies.Any(address => !IPAddress.TryParse(address, out _)) || api.ForwardLimit <= 0 ||
            api.JsonBodyLimitBytes <= 0 || api.MultipartBodyLimitBytes <= 5 * 1024 * 1024 ||
            api.MaximumJsonDepth is <= 0 or > 64)
        {
            throw new InvalidOperationException("Required Api deployment, request limit or proxy configuration is invalid.");
        }

        var policies = new[]
        {
            rates.PublicReads, rates.Authentication, rates.ReservationWrites, rates.AdminWrites, rates.FileUploads
        };

        if (policies.Any(policy => policy.PermitLimit <= 0 || policy.WindowSeconds <= 0))
        {
            throw new InvalidOperationException("All RateLimits categories require positive PermitLimit and WindowSeconds.");
        }
    }

    private static bool IsHttpsOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrEmpty(uri.Host) && uri.UserInfo.Length == 0 && uri.Query.Length == 0 &&
        uri.Fragment.Length == 0 && uri.AbsolutePath == "/" &&
        value == uri.GetLeftPart(UriPartial.Authority);
}
