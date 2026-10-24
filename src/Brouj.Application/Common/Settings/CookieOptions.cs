namespace Brouj.Application.Common.Settings;

public sealed class CookieOptions
{
    public string AccessTokenCookieName { get; init; } = "__Host-brouj-access";
    public string RefreshTokenCookieName { get; init; } = "__Host-brouj-refresh";
    public string SameSite { get; init; } = "Strict";
    public string Path { get; init; } = "/";
}
