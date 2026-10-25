
namespace Brouj.Application.Features.StageTemplates.DTOs;

/// <summary>Explicit Stage Template Item Dto contract; excludes persistence and credential secrets.</summary>
public sealed class StageTemplateItemDto
{
    public Guid ItemTemplateId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int SortOrder { get; init; }
}
