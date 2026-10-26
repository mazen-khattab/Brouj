using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;

public sealed class ChangePlanStageItemStatusCommandHandler : IRequestHandler<ChangePlanStageItemStatusCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public ChangePlanStageItemStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(
        ChangePlanStageItemStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.PlanStageItems.SingleOrDefaultAsync(item => item.Id == request.Id && item.StageId == request.StageId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        // Every defined status can follow every other status. Completion dates are supplied by the admin.
        entity.Status = request.Status;
        entity.ConfirmedDate = request.ConfirmedDate;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
