using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStage;

public sealed class DeleteTimePlanStageCommandHandler : IRequestHandler<DeleteTimePlanStageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;
    private readonly TimePlanOrdering _ordering;

    public DeleteTimePlanStageCommandHandler(
        IApplicationDbContext db,
        PendingFileCleanup cleanup,
        TimePlanOrdering ordering)
    {
        _db = db;
        _cleanup = cleanup;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(DeleteTimePlanStageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TimePlanStages.SingleOrDefaultAsync(row => row.Id == request.Id && row.TimePlanId == request.TimePlanId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var paths = await _db.StageItemImages.AsNoTracking()
            .Where(image => image.Item.StageId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);

        foreach (var path in paths)
        {
            _cleanup.Add(path);
        }

        _db.TimePlanStages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var siblings = await _db.TimePlanStages.Where(row => row.TimePlanId == request.TimePlanId)
            .OrderBy(row => row.SortOrder).ToListAsync(cancellationToken);

        await _ordering.StagesAsync(siblings, cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
