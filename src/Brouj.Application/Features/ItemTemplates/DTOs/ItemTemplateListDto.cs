
namespace Brouj.Application.Features.ItemTemplates.DTOs;

/// <summary>Explicit Item Template List Dto contract; excludes persistence and credential secrets.</summary>
public sealed class ItemTemplateListDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
