
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Stage Item Image Dto contract; excludes persistence and credential secrets.</summary>
public sealed class StageItemImageDto
{
    public Guid Id { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int SortOrder { get; init; }
}
