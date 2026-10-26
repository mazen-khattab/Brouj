using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;

[InvalidatesCache("time-plans")]
public sealed class ChangePlanStageItemStatusCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid StageId { get; init; }

    public Guid Id { get; init; }

    public PlanStageItemStatus Status { get; init; }

    public DateTimeOffset? ConfirmedDate { get; init; }
}
