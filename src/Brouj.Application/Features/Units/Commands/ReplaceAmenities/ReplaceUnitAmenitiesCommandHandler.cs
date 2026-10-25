using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.ReplaceAmenities;

public sealed class ReplaceUnitAmenitiesCommandHandler : IRequestHandler<ReplaceUnitAmenitiesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public ReplaceUnitAmenitiesCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(ReplaceUnitAmenitiesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.BaseUnits.AsNoTracking().AnyAsync(entity => entity.Id == request.UnitId, cancellationToken))
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        var previous = await _db.UnitAmenities.Where(amenity => amenity.UnitId == request.UnitId).ToListAsync(cancellationToken);

        _db.UnitAmenities.RemoveRange(previous);

        // One SaveChanges keeps replacement and auditing atomic.
        _db.UnitAmenities.AddRange(request.Amenities.Select(input => new UnitAmenity
        {
            Id = Guid.NewGuid(),
            UnitId = request.UnitId,
            Name = input.Name.Trim(),
            Value = input.Value
        }));

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.UnitId);
    }
}
