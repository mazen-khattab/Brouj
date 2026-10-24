using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Caching;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Abstractions.Storage;
using Brouj.Application.Common.Models;
using Brouj.Infrastructure.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using AppCookieOptions = Brouj.Application.Common.Settings.CookieOptions;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class CookieTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_authentication_handler_validates_bearer_or_cookie_and_enforces_actor_policies(bool cookie)
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var context = Context(scope.ServiceProvider);
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var token = jwt.GenerateCustomerAccessToken(Guid.NewGuid());
        if (cookie) context.Request.Headers.Cookie = "__Host-brouj-access=" + token;
        else context.Request.Headers.Authorization = "Bearer " + token;
        var authenticated = await context.AuthenticateAsync();
        Assert.True(authenticated.Succeeded);
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.True((await authorization.AuthorizeAsync(authenticated.Principal!, null, "Customer")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(authenticated.Principal!, null, "Staff")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(authenticated.Principal!, null, "SuperAdmin")).Succeeded);
    }

    [Fact]
    public async Task Staff_policy_accepts_staff_but_admin_cannot_use_super_admin_policy()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var context = Context(scope.ServiceProvider);
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        context.Request.Headers.Authorization = "Bearer " + jwt.GenerateUserAccessToken(Guid.NewGuid(), Brouj.Domain.Enums.UserRole.Admin);
        var authenticated = await context.AuthenticateAsync();
        Assert.True(authenticated.Succeeded);
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.True((await authorization.AuthorizeAsync(authenticated.Principal!, null, "Staff")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(authenticated.Principal!, null, "SuperAdmin")).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(authenticated.Principal!, null, "Customer")).Succeeded);
    }

    [Fact]
    public void All_approved_services_resolve_and_cookies_are_secure_host_only_and_http_only()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        Assert.NotNull(services.GetRequiredService<IPasswordHasher>());
        Assert.NotNull(services.GetRequiredService<IJwtTokenService>());
        Assert.NotNull(services.GetRequiredService<IRefreshTokenService>());
        Assert.NotNull(services.GetRequiredService<IFileStorageService>());
        Assert.NotNull(services.GetRequiredService<ICacheService>());
        Assert.NotNull(services.GetRequiredService<IApplicationTransactionManager>());
        var cookies = services.GetRequiredService<AuthenticationCookies>();
        var context = Context(services);
        cookies.Write(context, new RefreshTokenResult("synthetic-access", "synthetic-refresh", DateTimeOffset.UtcNow.AddDays(7)));
        var headers = context.Response.Headers.SetCookie;
        Assert.Equal(2, headers.Count);
        foreach (var value in headers)
        {
            Assert.Contains("secure", value);
            Assert.Contains("httponly", value);
            Assert.Contains("samesite=strict", value);
            Assert.Contains("path=/", value);
            Assert.DoesNotContain("domain=", value);
        }
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        Assert.Equal("RefreshTokenResult [redacted]", new RefreshTokenResult("secret-a", "secret-b", DateTimeOffset.UtcNow).ToString());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Unsafe_browser_requests_require_a_matching_antiforgery_cookie_and_request_token(string method)
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var antiforgery = services.GetRequiredService<IAntiforgery>();
        var cookies = services.GetRequiredService<AuthenticationCookies>();
        var bootstrap = Context(services);
        var csrf = cookies.GetCsrfToken(bootstrap);
        var csrfCookie = Assert.Single(bootstrap.Response.Headers.SetCookie)!.Split(';')[0];
        var reachedHandler = false;
        var middleware = new CookieCsrfMiddleware(_ => { reachedHandler = true; return Task.CompletedTask; });
        var request = Context(services);
        request.Request.Method = method;
        // A refresh cookie alone must never bypass CSRF when the access token is expired or absent.
        request.Request.Headers.Cookie = "__Host-brouj-refresh=synthetic; " + csrfCookie;
        await middleware.InvokeAsync(request, antiforgery);
        Assert.Equal(400, request.Response.StatusCode);
        Assert.False(reachedHandler);
        request = Context(services);
        request.Request.Method = method;
        request.Request.Headers.Cookie = csrfCookie;
        request.Request.Headers["X-CSRF-TOKEN"] = csrf;
        await middleware.InvokeAsync(request, antiforgery);
        Assert.True(reachedHandler);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task Safe_methods_do_not_require_csrf_validation(string method)
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var context = Context(scope.ServiceProvider);
        context.Request.Method = method;
        var reached = false;
        await new CookieCsrfMiddleware(_ => { reached = true; return Task.CompletedTask; })
            .InvokeAsync(context, scope.ServiceProvider.GetRequiredService<IAntiforgery>());
        Assert.True(reached);
    }

    [Fact]
    public async Task Antiforgery_tokens_work_with_minimal_JWT_claims_and_are_bound_to_the_actor()
    {
        using var provider = Provider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var jwt = services.GetRequiredService<IJwtTokenService>();
        var actor = jwt.ValidateAccessToken(jwt.GenerateCustomerAccessToken(Guid.NewGuid()));
        var bootstrap = Context(services);
        bootstrap.User = actor;
        var csrf = services.GetRequiredService<AuthenticationCookies>().GetCsrfToken(bootstrap);
        var csrfCookie = Assert.Single(bootstrap.Response.Headers.SetCookie)!.Split(';')[0];
        var request = Context(services);
        request.User = actor;
        request.Request.Method = "POST";
        request.Request.Headers.Cookie = csrfCookie;
        request.Request.Headers["X-CSRF-TOKEN"] = csrf;
        var reached = false;
        var middleware = new CookieCsrfMiddleware(_ => { reached = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(request, services.GetRequiredService<IAntiforgery>());
        Assert.True(reached);
        reached = false;
        request = Context(services);
        request.User = jwt.ValidateAccessToken(jwt.GenerateCustomerAccessToken(Guid.NewGuid()));
        request.Request.Method = "POST";
        request.Request.Headers.Cookie = csrfCookie;
        request.Request.Headers["X-CSRF-TOKEN"] = csrf;
        await middleware.InvokeAsync(request, services.GetRequiredService<IAntiforgery>());
        Assert.False(reached);
        Assert.Equal(400, request.Response.StatusCode);
    }

    [Fact]
    public void Invalid_jwt_options_fail_validation_without_echoing_the_secret()
    {
        using var provider = Provider("not-base64");
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<Brouj.Application.Common.Settings.JwtOptions>>().Value);
        Assert.DoesNotContain("not-base64", error.Message);
    }

    [Fact]
    public void External_cache_provider_is_preserved_instead_of_replaced_by_local_memory()
    {
        var services = new ServiceCollection();
        var external = new CacheTests.RecordingCache();
        services.AddSingleton<Microsoft.Extensions.Caching.Distributed.IDistributedCache>(external);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Caching:Provider"] = "External" }).Build();
        services.AddInfrastructureServices(configuration);
        // Resolve just the provider, rather than invalid optional JWT/file settings, to verify selection.
        using var provider = services.BuildServiceProvider();
        Assert.Same(external, provider.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>());
    }

    internal static ServiceProvider Provider(string? signingKey = null, string cacheProvider = "Memory")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "test-clients",
            ["Jwt:SigningKey"] = signingKey ?? AuthenticationTests.Settings().SigningKey,
            ["FileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "BroujServiceTests", Guid.NewGuid().ToString("N")),
            ["Caching:Provider"] = cacheProvider
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        // Unit tests keep their protection keys in memory; they never provision a host's persistent key ring.
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddInfrastructure("Server=localhost;Database=ModelOnly;Integrated Security=True;TrustServerCertificate=True;");
        services.AddInfrastructureServices(configuration);
        services.AddBroujCookieAuthentication();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static DefaultHttpContext Context(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("brouj.test");
        context.Response.Body = new MemoryStream();
        return context;
    }
}
