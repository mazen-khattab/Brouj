using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Queries.GetById;

public sealed class GetUnitByIdQueryHandler : IRequestHandler<GetUnitByIdQuery, Result<UnitDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUnitByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UnitDto>> Handle(GetUnitByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.BaseUnits.AsNoTracking()
            .Where(unit => unit.Id == request.Id && unit.ProjectId == request.ProjectId)
            .Select(unit => new UnitDto
            {
                Id = unit.Id,
                ProjectId = unit.ProjectId,
                Number = unit.Number,
                MeterPrice = unit.MeterPrice,
                FloorNumber = unit.FloorNumber,
                Size = unit.Size,
                TotalPrice = decimal.Ceiling(unit.Size * unit.MeterPrice * 100m) / 100m,
                NeighborMeterPrice = unit.InitiativeUnit == null ? null : unit.InitiativeUnit.NeighborMeterPrice,
                Percentage = unit.InitiativeUnit == null ? null : unit.InitiativeUnit.Percentage,
                IsAvailable = !unit.Reservations.Any(reservation => reservation.Order != null &&
                    reservation.Order.Status != OrderStatus.Pending)
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<UnitDto>.Failure(UnitErrors.NotFound);
        }

        // Load independent child collections separately to avoid a cartesian product.
        dto.Amenities = await _db.UnitAmenities.AsNoTracking()
            .Where(amenity => amenity.UnitId == request.Id)
            .OrderBy(amenity => amenity.Name).ThenBy(amenity => amenity.Id)
            .Select(amenity => new AmenityDto { Name = amenity.Name, Value = amenity.Value })
            .ToArrayAsync(cancellationToken);
        dto.Images = await _db.UnitImages.AsNoTracking()
            .Where(image => image.UnitId == request.Id).OrderBy(image => image.Id)
            .Select(image => new ImageMetadataDto { Id = image.Id, ImagePath = image.ImagePath, AltText = image.AltText })
            .ToArrayAsync(cancellationToken);

        return Result<UnitDto>.Success(dto);
    }
}
