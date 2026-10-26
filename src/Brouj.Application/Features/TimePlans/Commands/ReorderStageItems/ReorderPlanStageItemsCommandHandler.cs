using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.ReorderStageItems;

public sealed class ReorderPlanStageItemsCommandHandler : IRequestHandler<ReorderPlanStageItemsCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimePlanOrdering _ordering;

    public ReorderPlanStageItemsCommandHandler(IApplicationDbContext db, TimePlanOrdering ordering)
    {
        _db = db;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(ReorderPlanStageItemsCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.TimePlanStages.AsNoTracking().AnyAsync(parent => parent.Id == request.StageId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var rows = await _db.PlanStageItems.Where(row => row.StageId == request.StageId).ToListAsync(cancellationToken);
        var ids = request.Items.Select(item => item.Id).ToHashSet();

        if (rows.Count != ids.Count || rows.Any(row => !ids.Contains(row.Id)))
        {
            return Result<Guid>.Failure(TimePlanErrors.Validation("Supply each sibling exactly once; every item must belong to this parent."));
        }

        var positions = request.Items.ToDictionary(item => item.Id, item => item.SortOrder);
        var ordered = rows.OrderBy(row => positions[row.Id]).ToArray();

        await _ordering.ItemsAsync(ordered, cancellationToken);

        return Result<Guid>.Success(request.StageId);
    }
}
