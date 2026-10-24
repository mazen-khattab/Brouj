using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Caching;
using Brouj.Application.Abstractions.Storage;
using Brouj.Application.Common.Settings;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.Caching;
using Brouj.Infrastructure.Storage;
using Brouj.Infrastructure.Interceptors;
using Brouj.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using AppCookieOptions = Brouj.Application.Common.Settings.CookieOptions;

namespace Brouj.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers SQL Server persistence without changing any approved model configuration.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<ActivityLogInterceptor>();
        services.AddDbContextFactory<ApplicationDbContext>(options => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            // Provider errors can contain duplicate credential digests or other protected values.
            // Retain safe operational diagnostics without logging SQL error exception bodies.
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.SaveChangesFailed, RelationalEventId.CommandError)),
            ServiceLifetime.Scoped);
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationTransactionManager, ApplicationTransactionManager>();

        return services;
    }

    /// <summary>Registers approved Part 4 services. Supply secrets through configuration/environment, never source code.</summary>
    /// <remarks>Call alongside AddInfrastructure(connectionString). Caching:Provider defaults to Memory
    /// for development; External preserves a host-registered IDistributedCache such as Redis.</remarks>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection("Jwt"))
            .Validate(o => ValidJwt(o), "Invalid JWT signing configuration.").ValidateOnStart();
        services.AddOptions<FileStorageOptions>().Bind(configuration.GetSection("FileStorage"));
        services.AddOptions<LocalFileStorageOptions>().Bind(configuration.GetSection("FileStorage"))
            .Validate(o => Path.IsPathFullyQualified(o.RootPath), "An absolute storage root is required.").ValidateOnStart();
        services.AddOptions<CacheOptions>().Bind(configuration.GetSection("Caching"))
            .Validate(o => o.DefaultExpirationSeconds is > 0 and <= 86400, "Invalid cache expiration.").ValidateOnStart();
        services.AddOptions<AppCookieOptions>().Bind(configuration.GetSection("AuthenticationCookies"))
            .Validate(o => ValidCookies(o), "Invalid authentication cookie policy.").ValidateOnStart();
        var provider = configuration["Caching:Provider"] ?? "Memory";
        if (provider == "Memory") services.AddDistributedMemoryCache();
        else if (provider != "External") throw new InvalidOperationException("Caching provider must be Memory or External.");
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddScoped<AuthenticationCookies>();
        services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-CSRF-TOKEN";
            o.Cookie.Name = "__Host-brouj-csrf";
            o.Cookie.HttpOnly = true; 
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Strict; 
            o.Cookie.Path = "/";
        });
        services.AddOptions<AntiforgeryOptions>().Configure<IOptions<AppCookieOptions>>((csrf, cookies) =>
            csrf.Cookie.SameSite = AuthenticationCookies.ValidatedOptions(cookies.Value).SameSite);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, clock) =>
            {
                bearer.MapInboundClaims = false;
                bearer.IncludeErrorDetails = false;
                bearer.SaveToken = false;
                bearer.TokenValidationParameters = JwtTokenService.ValidationParameters(jwt.Value, clock);
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        try { JwtTokenService.ValidateClaims(context.Principal!, clock); }
                        catch (Microsoft.IdentityModel.Tokens.SecurityTokenException) { context.Fail("Invalid access token."); }
                        return Task.CompletedTask;
                    }
                };
            });
        services.AddRouting();
        services.AddAuthorization(o =>
        {
            o.AddPolicy("Customer", p => p.RequireAuthenticatedUser().RequireClaim("actor_type", "customer"));
            o.AddPolicy("Staff", p => p.RequireAuthenticatedUser().RequireClaim("actor_type", "user").RequireRole("admin", "super admin"));
            o.AddPolicy("SuperAdmin", p => p.RequireAuthenticatedUser().RequireClaim("actor_type", "user").RequireRole("super admin"));
        });
        return services;
    }

    /// <summary>Opts a browser host into cookie JWT authentication; pair with UseBroujCookieProtection.</summary>
    public static IServiceCollection AddBroujCookieAuthentication(this IServiceCollection services)
    {
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events.OnMessageReceived = context =>
            {
                // Explicit Authorization headers take precedence; never copy cookie values into logs.
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    var cookies = context.HttpContext.RequestServices.GetRequiredService<IOptions<AppCookieOptions>>().Value;
                    context.Token = context.Request.Cookies[cookies.AccessTokenCookieName];
                }
                return Task.CompletedTask;
            };
        });
        return services;
    }

    /// <summary>Places CSRF validation after authentication and before authorization/endpoints in a browser host.</summary>
    public static IApplicationBuilder UseBroujCookieProtection(this IApplicationBuilder app) => app.UseMiddleware<CookieCsrfMiddleware>();

    private static bool ValidJwt(JwtOptions options)
    {
        try { _ = JwtTokenService.SigningKey(options); return true; }
        catch (InvalidOperationException) { return false; }
    }

    private static bool ValidCookies(AppCookieOptions options)
    {
        try { _ = AuthenticationCookies.ValidatedOptions(options); return true; }
        catch (InvalidOperationException) { return false; }
    }
}
