
namespace Brouj.Application.Features.StageTemplates.DTOs;

/// <summary>Explicit Stage Template Item Input contract; excludes persistence and credential secrets.</summary>
public sealed class StageTemplateItemInput
{
    public Guid ItemTemplateId { get; init; }

    public int SortOrder { get; init; }
}
