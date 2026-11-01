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
    public string FullName { get; init; } = string.Empty;
    public string AgeGroup { get; init; } = string.Empty;
    public string IdentityNumber { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string PlaceOfResidence { get; init; } = string.Empty;
    public int PreferredFinancialBracket { get; set; }
    public int PreferredRepaymentPeriod { get; set; }
}
