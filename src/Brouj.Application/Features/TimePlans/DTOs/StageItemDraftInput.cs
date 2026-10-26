
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Stage Item Draft Input contract; excludes persistence and credential secrets.</summary>
public sealed class StageItemDraftInput
{
    public Guid? ItemTemplateId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset? DueDate { get; init; }

    public DateTimeOffset? ConfirmedDate { get; init; }
}
