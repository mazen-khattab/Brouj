using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class TimePlanStageExtensions
{
    public static string ToDatabaseString(this TimePlanStageStatus value) => value switch
    {
        TimePlanStageStatus.Pending => "pending",
        TimePlanStageStatus.Processing => "processing",
        TimePlanStageStatus.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown time plan stage status.")
    };

    public static TimePlanStageStatus ToOrderStatus(this string value) => value switch
    {
        "pending" => TimePlanStageStatus.Pending,
        "processing" => TimePlanStageStatus.Processing,
        "completed" => TimePlanStageStatus.Completed,
        _ => throw new ArgumentException("Unknown database value for time plan stage status.", nameof(value))
    };
}
