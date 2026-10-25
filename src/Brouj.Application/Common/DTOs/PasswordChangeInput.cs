
namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Password Change Input contract; excludes persistence and credential secrets.</summary>
public sealed class PasswordChangeInput
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
