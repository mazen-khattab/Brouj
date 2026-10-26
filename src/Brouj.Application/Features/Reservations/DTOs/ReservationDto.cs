using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Reservations.DTOs;

/// <summary>Explicit Reservation Dto contract; excludes persistence and credential secrets.</summary>
public sealed class ReservationDto
{
    public Guid Id { get; init; }

    public Guid UnitId { get; init; }

    public string UnitNumber { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public string Number { get; init; } = string.Empty;

    public ReservationStatus Status { get; init; }

    public DateTimeOffset ReservationDate { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public Guid? OrderId { get; init; }
}
