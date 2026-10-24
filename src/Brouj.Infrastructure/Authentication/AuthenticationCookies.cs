using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AppCookieOptions = Brouj.Application.Common.Settings.CookieOptions;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Issues secure credential cookies and framework antiforgery request tokens for browser clients.</summary>
public sealed class AuthenticationCookies(IOptions<AppCookieOptions> options, IAntiforgery antiforgery, TimeProvider clock)
{
    /// <summary>Writes a successful session using HttpOnly, Secure, host-only cookies.</summary>
    public void Write(HttpContext context, RefreshTokenResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var cookie = ValidatedOptions(options.Value);
        context.Response.Cookies.Append(options.Value.AccessTokenCookieName, result.AccessToken, Copy(cookie, clock.GetUtcNow().AddMinutes(15)));
        context.Response.Cookies.Append(options.Value.RefreshTokenCookieName, result.RefreshToken, Copy(cookie, result.ExpiresAt));
        context.Response.Headers.CacheControl = "no-store";
    }

    /// <summary>Deletes both credentials with the same path and security attributes used to create them.</summary>
    public void Clear(HttpContext context)
    {
        var cookie = ValidatedOptions(options.Value);
        context.Response.Cookies.Delete(options.Value.AccessTokenCookieName, cookie);
        context.Response.Cookies.Delete(options.Value.RefreshTokenCookieName, cookie);
        context.Response.Headers.CacheControl = "no-store";
    }

    /// <summary>Creates the framework's cookie/request-token pair; return the request token in a no-store response.</summary>
    /// <remarks>Clients send this value in X-CSRF-TOKEN for unsafe requests, including login, refresh and logout.
    /// Retrieve a fresh token after authentication changes; antiforgery tokens are bound to the current identity.</remarks>
    public string GetCsrfToken(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        return antiforgery.GetAndStoreTokens(context).RequestToken
            ?? throw new InvalidOperationException("Antiforgery token generation failed.");
    }

    internal static CookieOptions ValidatedOptions(AppCookieOptions options)
    {
        if (!options.AccessTokenCookieName.StartsWith("__Host-", StringComparison.Ordinal) ||
            !options.RefreshTokenCookieName.StartsWith("__Host-", StringComparison.Ordinal) ||
            options.AccessTokenCookieName == options.RefreshTokenCookieName ||
            options.Path != "/" ||
            options.AccessTokenCookieName.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_') ||
            options.RefreshTokenCookieName.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_') ||
            !Enum.TryParse<SameSiteMode>(options.SameSite, false, out var sameSite) ||
            sameSite is not (SameSiteMode.Strict or SameSiteMode.Lax or SameSiteMode.None))
            throw new InvalidOperationException("Invalid authentication cookie policy.");

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Path = "/",
            IsEssential = true
        };
    }

    private static CookieOptions Copy(CookieOptions source, DateTimeOffset expiry) => new()
    {
        HttpOnly = source.HttpOnly,
        Secure = source.Secure,
        SameSite = source.SameSite,
        Path = source.Path,
        IsEssential = true,
        Expires = expiry
    };
}
