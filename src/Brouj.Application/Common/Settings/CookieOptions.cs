namespace Brouj.Application.Common.Settings;

public sealed class CookieOptions
{
    public string AccessTokenCookieName { get; init; } = string.Empty;
    public string RefreshTokenCookieName { get; init; } = string.Empty;
    public string SameSite { get; init; } = string.Empty;
    public string Path { get; init; } = "/";
}
