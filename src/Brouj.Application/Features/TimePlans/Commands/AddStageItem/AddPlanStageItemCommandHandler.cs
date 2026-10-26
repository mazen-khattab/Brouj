using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageItem;

public sealed class AddPlanStageItemCommandHandler : IRequestHandler<AddPlanStageItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public AddPlanStageItemCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(AddPlanStageItemCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.TimePlanStages.AsNoTracking().AnyAsync(stage => stage.Id == request.StageId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var last = await _db.PlanStageItems.Where(item => item.StageId == request.StageId)
            .Select(item => (int?)item.SortOrder).MaxAsync(cancellationToken) ?? 0;
        var entity = new PlanStageItem
        {
            Id = Guid.NewGuid(),
            StageId = request.StageId,
            SortOrder = last + 1,
            Status = PlanStageItemStatus.Pending
        };

        entity.Name = request.Input.Name.Trim();
        entity.DueDate = request.Input.DueDate!.Value;
        entity.ConfirmedDate = request.Input.ConfirmedDate;

        _db.PlanStageItems.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
