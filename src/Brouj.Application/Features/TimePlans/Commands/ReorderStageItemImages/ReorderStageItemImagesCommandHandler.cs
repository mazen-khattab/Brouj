using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.ReorderStageItemImages;

public sealed class ReorderStageItemImagesCommandHandler : IRequestHandler<ReorderStageItemImagesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimePlanOrdering _ordering;

    public ReorderStageItemImagesCommandHandler(IApplicationDbContext db, TimePlanOrdering ordering)
    {
        _db = db;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(ReorderStageItemImagesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.PlanStageItems.AsNoTracking().AnyAsync(parent => parent.Id == request.ItemId, cancellationToken))
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var rows = await _db.StageItemImages.Where(row => row.ItemId == request.ItemId).ToListAsync(cancellationToken);
        var ids = request.Items.Select(item => item.Id).ToHashSet();

        if (rows.Count != ids.Count || rows.Any(row => !ids.Contains(row.Id)))
        {
            return Result<Guid>.Failure(TimePlanErrors.Validation("Supply each sibling exactly once; every item must belong to this parent."));
        }

        var positions = request.Items.ToDictionary(item => item.Id, item => item.SortOrder);
        var ordered = rows.OrderBy(row => positions[row.Id]).ToArray();

        await _ordering.ImagesAsync(ordered, cancellationToken);

        return Result<Guid>.Success(request.ItemId);
    }
}
