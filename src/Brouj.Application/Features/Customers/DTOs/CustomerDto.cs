using Brouj.Application.Common.Utils;
using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Customers.DTOs;

/// <summary>Explicit Customer Dto contract; excludes persistence and credential secrets.</summary>
public sealed class CustomerDto
{
    public Guid Id { get; init; }

    public string FName { get; init; } = string.Empty;

    public string LName { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTimeOffset? DeletedAt { get; init; }
}
