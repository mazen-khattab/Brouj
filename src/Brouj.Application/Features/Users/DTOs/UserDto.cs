using Brouj.Application.Common.Utils;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Users.DTOs;

/// <summary>Explicit User Dto contract; excludes persistence and credential secrets.</summary>
public sealed class UserDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public DateTimeOffset? DeletedAt { get; init; }
}
