using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStage;

[InvalidatesCache("time-plans")]
public sealed class DeleteTimePlanStageCommand : IRequest<Result<Guid>>, ITransactionalRequest, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid TimePlanId { get; init; }

    public Guid Id { get; init; }
}
