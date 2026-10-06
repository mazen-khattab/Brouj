using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class PlanStageItemStatusExtensions
{
    public static string ToDatabaseString(this PlanStageItemStatus value) => value switch
    {
        PlanStageItemStatus.Pending => "pending",
        PlanStageItemStatus.Processing => "processing",
        PlanStageItemStatus.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown plan stage item status.")
    };

    public static PlanStageItemStatus ToPlanStageItemStatus(this string value) => value switch
    {
        "pending" => PlanStageItemStatus.Pending,
        "processing" => PlanStageItemStatus.Processing,
        "completed" => PlanStageItemStatus.Completed,
        _ => throw new ArgumentException("Unknown database value for plan stage item status.", nameof(value))
    };
}
