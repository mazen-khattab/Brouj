using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Units.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.DeleteImage;

public sealed class DeleteUnitImageCommandHandler : IRequestHandler<DeleteUnitImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly PendingFileCleanup _cleanup;

    public DeleteUnitImageCommandHandler(IApplicationDbContext db, PendingFileCleanup cleanup)
    {
        _db = db;
        _cleanup = cleanup;
    }

    public async Task<Result<Guid>> Handle(DeleteUnitImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.UnitImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.UnitId == request.UnitId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        _cleanup.Add(entity.ImagePath);
        _db.UnitImages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
