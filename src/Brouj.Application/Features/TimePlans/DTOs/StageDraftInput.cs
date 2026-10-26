
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Stage Draft Input contract; excludes persistence and credential secrets.</summary>
public sealed class StageDraftInput
{
    public Guid? StageTemplateId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset? DeliveryDate { get; init; }

    public IReadOnlyList<StageItemDraftInput> Items { get; init; } = [];
}
