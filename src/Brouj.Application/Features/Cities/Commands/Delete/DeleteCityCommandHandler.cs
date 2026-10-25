using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Cities.Commands.Delete;

public sealed class DeleteCityCommandHandler : IRequestHandler<DeleteCityCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public DeleteCityCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Cities.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(CityErrors.NotFound);
        }

        if (await _db.Areas.AsNoTracking().AnyAsync(area => area.CityId == entity.Id, cancellationToken))
        {
            return Result<Guid>.Failure(CityErrors.Conflict("The resource still has dependent areas."));
        }

        // The approved database cascade removes area amenities.
        _db.Cities.Remove(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
