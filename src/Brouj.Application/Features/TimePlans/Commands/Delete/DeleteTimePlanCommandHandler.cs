using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.Delete;

public sealed class DeleteTimePlanCommandHandler : IRequestHandler<DeleteTimePlanCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;

    public DeleteTimePlanCommandHandler(IApplicationDbContext db, PendingFileCleanup cleanup)
    {
        _db = db;
        _cleanup = cleanup;
    }

    public async Task<Result<Guid>> Handle(DeleteTimePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TimePlans.SingleOrDefaultAsync(plan => plan.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        var paths = await _db.StageItemImages.AsNoTracking().Where(image => image.Item.Stage.TimePlanId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);

        foreach (var path in paths)
        {
            _cleanup.Add(path);
        }

        _db.TimePlans.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
