using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStageItem;

[InvalidatesCache("time-plans")]
public sealed class DeletePlanStageItemCommand : IRequest<Result<Guid>>, ITransactionalRequest, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid StageId { get; init; }

    public Guid Id { get; init; }
}
