using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class UserRoleExtensions
{
    public static string ToDatabaseString(this UserRole value) => value switch
    {
        UserRole.SuperAdmin => "super admin",
        UserRole.Admin => "admin",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown user role.")
    };

    public static UserRole ToUserRole(this string value) => value switch
    {
        "super admin" => UserRole.SuperAdmin,
        "admin" => UserRole.Admin,
        _ => throw new ArgumentException("Unknown database value for user role.", nameof(value))
    };
}
