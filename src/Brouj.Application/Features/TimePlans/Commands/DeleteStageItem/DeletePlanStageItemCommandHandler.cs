using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStageItem;

public sealed class DeletePlanStageItemCommandHandler : IRequestHandler<DeletePlanStageItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;
    private readonly TimePlanOrdering _ordering;

    public DeletePlanStageItemCommandHandler(
        IApplicationDbContext db,
        PendingFileCleanup cleanup,
        TimePlanOrdering ordering)
    {
        _db = db;
        _cleanup = cleanup;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(DeletePlanStageItemCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.PlanStageItems.SingleOrDefaultAsync(row => row.Id == request.Id && row.StageId == request.StageId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var paths = await _db.StageItemImages.AsNoTracking()
            .Where(image => image.ItemId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);

        foreach (var path in paths)
        {
            _cleanup.Add(path);
        }

        _db.PlanStageItems.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var siblings = await _db.PlanStageItems.Where(row => row.StageId == request.StageId)
            .OrderBy(row => row.SortOrder).ToListAsync(cancellationToken);

        await _ordering.ItemsAsync(siblings, cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
