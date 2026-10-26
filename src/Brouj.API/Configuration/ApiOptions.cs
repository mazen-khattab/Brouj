namespace Brouj.API.Configuration;

/// <summary>Deployment inputs have no guessed traffic limits or implicitly trusted proxies.</summary>
public sealed class ApiOptions
{
    public string BaseUrl { get; init; } = string.Empty;
    public string[] AllowedOrigins { get; init; } = [];
    public string[] TrustedProxies { get; init; } = [];
    public int ForwardLimit { get; init; } = 1;
    public long JsonBodyLimitBytes { get; init; }
    public long MultipartBodyLimitBytes { get; init; }
    public int MaximumJsonDepth { get; init; }
}
