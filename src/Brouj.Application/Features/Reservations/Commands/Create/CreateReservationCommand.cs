using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Reservations.Commands.Create;

public sealed class CreateReservationCommand : IRequest<Result<Guid>>, IRequireAccess, ITransactionalRequest
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

    public Guid UnitId { get; init; }
}
