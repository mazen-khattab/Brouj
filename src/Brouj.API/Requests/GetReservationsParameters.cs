using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetReservationsParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public ReservationStatus? Status { get; init; }
}
