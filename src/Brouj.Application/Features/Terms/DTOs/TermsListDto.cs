
namespace Brouj.Application.Features.Terms.DTOs;

/// <summary>Explicit Terms List Dto contract; excludes persistence and credential secrets.</summary>
public sealed class TermsListDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public int VersionNumber { get; init; }

    public DateTimeOffset EffectiveDate { get; init; }

    public bool IsActive { get; init; }
}
