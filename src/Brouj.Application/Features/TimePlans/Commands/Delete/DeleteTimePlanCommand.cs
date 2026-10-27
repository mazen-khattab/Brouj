using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.Delete;

[InvalidatesCache("time-plans")]
public sealed class DeleteTimePlanCommand : IRequest<Result<Guid>>, IRequireAccess, ITransactionalRequest
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
