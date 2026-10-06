using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class ActivityActionExtensions
{
    public static string ToDatabaseString(this ActivityAction value) => value switch
    {
        ActivityAction.Created => "created",
        ActivityAction.Updated => "updated",
        ActivityAction.Deleted => "deleted",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown activity action.")
    };

    public static ActivityAction ToActivityAction(this string value) => value switch
    {
        "created" => ActivityAction.Created,
        "updated" => ActivityAction.Updated,
        "deleted" => ActivityAction.Deleted,
        _ => throw new ArgumentException("Unknown database value for activity action.", nameof(value))
    };
}
