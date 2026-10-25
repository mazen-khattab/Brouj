using Brouj.Domain.Enums;

namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Plan Stage Item Dto contract; excludes persistence and credential secrets.</summary>
public sealed class PlanStageItemDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public PlanStageItemStatus Status { get; init; }

    public int SortOrder { get; init; }

    public DateTimeOffset DueDate { get; init; }

    public DateTimeOffset? ConfirmedDate { get; init; }

    public IReadOnlyList<StageItemImageDto> Images { get; init; } = [];
}
