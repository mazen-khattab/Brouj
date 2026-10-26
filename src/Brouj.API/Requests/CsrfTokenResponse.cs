namespace Brouj.API.Requests;

/// <summary>Send this framework proof in X-CSRF-TOKEN; the paired cookie remains HttpOnly.</summary>
public sealed class CsrfTokenResponse
{
    public required string RequestToken { get; init; }
}
