using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStageItem;

public sealed class UpdatePlanStageItemCommandHandler : IRequestHandler<UpdatePlanStageItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdatePlanStageItemCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdatePlanStageItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.PlanStageItems.SingleOrDefaultAsync(item => item.Id == request.Id && item.StageId == request.StageId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        if (entity.Status == PlanStageItemStatus.Completed && request.Input.ConfirmedDate is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.Validation("Pls insert the dates first"));
        }

        entity.Name = request.Input.Name.Trim();
        entity.DueDate = request.Input.DueDate!.Value;
        entity.ConfirmedDate = request.Input.ConfirmedDate;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
