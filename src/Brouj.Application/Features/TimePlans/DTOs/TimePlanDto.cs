
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Time Plan Dto contract; excludes persistence and credential secrets.</summary>
public sealed class TimePlanDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset DeliveryDate { get; init; }

    public IReadOnlyList<TimePlanStageDto> Stages { get; init; } = [];
}
