using Brouj.Application.Common.Utils;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Users.DTOs;

/// <summary>Explicit Update User Input contract; excludes persistence and credential secrets.</summary>
public sealed class UpdateUserInput
{
    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public UserRole Role { get; init; }
}
