using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Orders.DTOs;

/// <summary>Explicit Order Dto contract; excludes persistence and credential secrets.</summary>
public sealed class OrderDto
{
    public Guid Id { get; init; }

    public Guid ReservationId { get; init; }

    public Guid UnitId { get; init; }

    public string UnitNumber { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public string Number { get; init; } = string.Empty;

    public OrderStatus Status { get; init; }

    public decimal TotalPrice { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}
