using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class AuthenticationSessionTests
{
    [Fact]
    public void Session_adapter_reads_the_presented_cookie_and_uses_the_secure_cookie_writer()
    {
        using var provider = CookieTests.Provider();
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers.Cookie = "__Host-brouj-refresh=synthetic-presented";
        accessor.HttpContext = context;
        var session = services.GetRequiredService<IAuthenticationSession>();

        Assert.Equal("synthetic-presented", session.ReadRefreshToken());

        session.Write(new RefreshTokenResult("synthetic-access", "synthetic-refresh", DateTimeOffset.UtcNow.AddDays(7)));

        Assert.Equal(2, context.Response.Headers.SetCookie.Count);
        Assert.All(context.Response.Headers.SetCookie, cookie =>
        {
            Assert.Contains("secure", cookie);
            Assert.Contains("httponly", cookie);
            Assert.Contains("path=/", cookie);
        });
        Assert.Equal("no-store", context.Response.Headers.CacheControl);

        // Start a fresh response so removal headers cannot be confused with the preceding issuance.
        context = new DefaultHttpContext { RequestServices = services };
        accessor.HttpContext = context;

        session.Clear();

        Assert.Equal(2, context.Response.Headers.SetCookie.Count);
        Assert.All(context.Response.Headers.SetCookie, cookie => Assert.Contains("expires=", cookie));
        Assert.Equal("no-store", context.Response.Headers.CacheControl);
        accessor.HttpContext = null;
    }
}
