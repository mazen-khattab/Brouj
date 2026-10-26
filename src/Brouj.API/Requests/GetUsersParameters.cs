using Brouj.Application.Features.Users.DTOs;
using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetUsersParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public bool? IsDeleted { get; init; }

    public UserRole? Role { get; init; }
}
