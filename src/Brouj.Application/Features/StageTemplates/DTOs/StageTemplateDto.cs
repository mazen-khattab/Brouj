
namespace Brouj.Application.Features.StageTemplates.DTOs;

/// <summary>Explicit Stage Template Dto contract; excludes persistence and credential secrets.</summary>
public sealed class StageTemplateDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public IReadOnlyList<StageTemplateItemDto> Items { get; init; } = [];
}
