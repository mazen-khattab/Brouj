using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Reservations.Queries.GetAll;

public sealed class GetReservationsQuery : IRequest<Result<PaginationResult<ReservationDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public ReservationStatus? Status { get; init; }
}
