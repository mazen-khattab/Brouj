using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using Brouj.Application.Features.Areas.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Queries.GetById;

public sealed class GetAreaByIdQueryHandler : IRequestHandler<GetAreaByIdQuery, Result<AreaDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAreaByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AreaDto>> Handle(GetAreaByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Areas.AsNoTracking().Where(area => area.Id == request.Id)
            .Select(area => new AreaDto
            {
                Id = area.Id,
                Name = area.Name,
                CityId = area.CityId,
                CityName = area.City.Name,
                Amenities = area.AreaAmenities.OrderBy(amenity => amenity.Name).ThenBy(amenity => amenity.Id)
                    .Select(amenity => new AmenityDto { Name = amenity.Name, Value = amenity.Value }).ToArray()
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<AreaDto>.Failure(AreaErrors.NotFound);
        }

        return Result<AreaDto>.Success(dto);
    }
}
