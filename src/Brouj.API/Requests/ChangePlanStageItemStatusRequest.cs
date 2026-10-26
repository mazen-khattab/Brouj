using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class ChangePlanStageItemStatusRequest
{
    public PlanStageItemStatus Status { get; init; }

    public DateTimeOffset? ConfirmedDate { get; init; }
}
