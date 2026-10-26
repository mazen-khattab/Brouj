
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Time Plan Stage Dto contract; excludes persistence and credential secrets.</summary>
public sealed class TimePlanStageDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public DateTimeOffset DeliveryDate { get; init; }

    public IReadOnlyList<PlanStageItemDto> Items { get; init; } = [];
}
