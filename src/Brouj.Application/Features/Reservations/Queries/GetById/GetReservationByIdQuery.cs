using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.DTOs;
using MediatR;

namespace Brouj.Application.Features.Reservations.Queries.GetById;

public sealed class GetReservationByIdQuery : IRequest<Result<ReservationDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

    public Guid Id { get; init; }
}
