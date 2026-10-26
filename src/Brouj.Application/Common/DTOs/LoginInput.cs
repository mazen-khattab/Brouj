using Brouj.Application.Common.Utils;

namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Login Input contract; excludes persistence and credential secrets.</summary>
public sealed class LoginInput
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
