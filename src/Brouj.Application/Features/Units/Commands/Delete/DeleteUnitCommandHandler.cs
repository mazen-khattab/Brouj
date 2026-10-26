using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Units.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.Delete;

public sealed class DeleteUnitCommandHandler : IRequestHandler<DeleteUnitCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;

    public DeleteUnitCommandHandler(IApplicationDbContext db, PendingFileCleanup cleanup)
    {
        _db = db;
        _cleanup = cleanup;
    }

    public async Task<Result<Guid>> Handle(DeleteUnitCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.BaseUnits.SingleOrDefaultAsync(unit => unit.Id == request.Id && unit.ProjectId == request.ProjectId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        if (await _db.Reservations.AsNoTracking().AnyAsync(reservation => reservation.UnitId == entity.Id, cancellationToken))
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("A unit with any reservation cannot be deleted."));
        }

        var paths = await _db.UnitImages.AsNoTracking().Where(image => image.UnitId == entity.Id)
            .Select(image => image.ImagePath).ToListAsync(cancellationToken);

        foreach (var path in paths)
        {
            _cleanup.Add(path);
        }

        _db.BaseUnits.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
