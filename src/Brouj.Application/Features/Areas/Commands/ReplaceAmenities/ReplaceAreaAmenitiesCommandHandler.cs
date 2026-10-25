using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Commands.ReplaceAmenities;

public sealed class ReplaceAreaAmenitiesCommandHandler : IRequestHandler<ReplaceAreaAmenitiesCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public ReplaceAreaAmenitiesCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(ReplaceAreaAmenitiesCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.Areas.AsNoTracking().AnyAsync(entity => entity.Id == request.AreaId, cancellationToken))
        {
            return Result<Guid>.Failure(AreaErrors.NotFound);
        }

        var previous = await _db.AreaAmenities.Where(amenity => amenity.AreaId == request.AreaId).ToListAsync(cancellationToken);

        _db.AreaAmenities.RemoveRange(previous);

        // One SaveChanges keeps replacement and auditing atomic.
        _db.AreaAmenities.AddRange(request.Amenities.Select(input => new AreaAmenity
        {
            Id = Guid.NewGuid(),
            AreaId = request.AreaId,
            Name = input.Name.Trim(),
            Value = input.Value
        }));

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.AreaId);
    }
}
