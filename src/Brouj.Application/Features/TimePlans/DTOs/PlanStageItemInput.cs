
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Plan Stage Item Input contract; excludes persistence and credential secrets.</summary>
public sealed class PlanStageItemInput
{
    public string Name { get; init; } = string.Empty;

    public DateTimeOffset? DueDate { get; init; }

    public DateTimeOffset? ConfirmedDate { get; init; }
}
