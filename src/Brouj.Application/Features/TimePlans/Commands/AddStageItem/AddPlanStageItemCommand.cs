using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageItem;

[InvalidatesCache("time-plans")]
public sealed class AddPlanStageItemCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid StageId { get; init; }

    public PlanStageItemInput Input { get; init; } = new();
}
