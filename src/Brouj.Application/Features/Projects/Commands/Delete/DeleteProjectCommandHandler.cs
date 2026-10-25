using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Projects.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Commands.Delete;

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;

    public DeleteProjectCommandHandler(IApplicationDbContext db, PendingFileCleanup cleanup)
    {
        _db = db;
        _cleanup = cleanup;
    }

    public async Task<Result<Guid>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.BaseProjects.SingleOrDefaultAsync(project => project.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(ProjectErrors.NotFound);
        }

        if (await _db.Reservations.AsNoTracking().AnyAsync(reservation => reservation.Unit.ProjectId == entity.Id, cancellationToken))
        {
            return Result<Guid>.Failure(ProjectErrors.HasReservations);
        }

        var projectPaths = await _db.ProjectImages.AsNoTracking().Where(image => image.ProjectId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);
        var unitPaths = await _db.UnitImages.AsNoTracking().Where(image => image.Unit.ProjectId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);
        var stagePaths = await _db.StageItemImages.AsNoTracking()
            .Where(image => image.Item.Stage.TimePlan.ProjectId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);

        foreach (var path in projectPaths.Concat(unitPaths).Concat(stagePaths))
        {
            _cleanup.Add(path);
        }

        // Approved SQL cascades remove the entire allowed project tree; reservations are never cascaded.
        _db.BaseProjects.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
