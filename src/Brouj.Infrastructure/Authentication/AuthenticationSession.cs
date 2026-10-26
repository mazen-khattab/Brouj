using AppCookieOptions = Brouj.Application.Common.Settings.CookieOptions;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Connects Application session operations to the existing secure cookie policy.</summary>
public sealed class AuthenticationSession : IAuthenticationSession
{
    private readonly IHttpContextAccessor _accessor;
    private readonly AuthenticationCookies _cookies;
    private readonly AppCookieOptions _options;

    public AuthenticationSession(
        IHttpContextAccessor accessor,
        AuthenticationCookies cookies,
        IOptions<AppCookieOptions> options)
    {
        _accessor = accessor;
        _cookies = cookies;
        _options = options.Value;
    }

    public string? ReadRefreshToken() => Context.Request.Cookies[_options.RefreshTokenCookieName];

    public void Write(RefreshTokenResult result) => _cookies.Write(Context, result);

    public void Clear() => _cookies.Clear(Context);

    private HttpContext Context => _accessor.HttpContext
        ?? throw new InvalidOperationException("An authentication session requires an active request.");
}
