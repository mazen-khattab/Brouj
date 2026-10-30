using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetUsersParameters : PagedSearchParameters
{
    public bool? IsDeleted { get; init; }

    public UserRole? Role { get; init; }
}
