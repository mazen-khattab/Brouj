using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.SoftDelete;

public sealed class SoftDeleteCustomerCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
