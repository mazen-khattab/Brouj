using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class ProjectTypeExtensions
{
    public static string ToDatabaseString(this ProjectType value) => value switch
    {
        ProjectType.Regular => "regular",
        ProjectType.Initiative => "initiative",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown project type.")
    };

    public static ProjectType ToProjectType(this string value) => value switch
    {
        "regular" => ProjectType.Regular,
        "initiative" => ProjectType.Initiative,
        _ => throw new ArgumentException("Unknown database value for project type.", nameof(value))
    };
}
