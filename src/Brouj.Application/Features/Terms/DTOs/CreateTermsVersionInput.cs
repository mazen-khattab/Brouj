
namespace Brouj.Application.Features.Terms.DTOs;

/// <summary>Explicit Create Terms Version Input contract; excludes persistence and credential secrets.</summary>
public sealed class CreateTermsVersionInput
{
    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int VersionNumber { get; init; }

    public DateTimeOffset EffectiveDate { get; init; }
}
