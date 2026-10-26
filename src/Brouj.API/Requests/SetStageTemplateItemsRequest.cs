using Brouj.Application.Features.StageTemplates.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class SetStageTemplateItemsRequest
{
    public IReadOnlyList<StageTemplateItemInput> Items { get; init; } = [];
}
