namespace Brouj.Application.Abstractions.Authentication;

public enum AccessLevel
{
    Customer,

    // Both Admin and SuperAdmin can perform staff operations.
    Staff,

    SuperAdmin,

    // Time plans and shared order details allow either an active customer or active staff.
    Member
}
