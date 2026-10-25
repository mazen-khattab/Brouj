using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.TimePlans.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStageItemImage;

public sealed class DeleteStageItemImageCommandHandler : IRequestHandler<DeleteStageItemImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;
    private readonly TimePlanOrdering _ordering;

    public DeleteStageItemImageCommandHandler(
        IApplicationDbContext db,
        PendingFileCleanup cleanup,
        TimePlanOrdering ordering)
    {
        _db = db;
        _cleanup = cleanup;
        _ordering = ordering;
    }

    public async Task<Result<Guid>> Handle(DeleteStageItemImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.StageItemImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.ItemId == request.ItemId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        _cleanup.Add(entity.ImagePath);
        _db.StageItemImages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var siblings = await _db.StageItemImages.Where(image => image.ItemId == request.ItemId)
            .OrderBy(image => image.SortOrder).ToListAsync(cancellationToken);

        await _ordering.ImagesAsync(siblings, cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
