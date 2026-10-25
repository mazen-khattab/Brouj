using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.DTOs;
using Brouj.Application.Features.Cities.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Cities.Queries.GetById;

public sealed class GetCityByIdQueryHandler : IRequestHandler<GetCityByIdQuery, Result<CityDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCityByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CityDto>> Handle(GetCityByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Cities.AsNoTracking().Where(city => city.Id == request.Id)
            .Select(city => new CityDto { Id = city.Id, Name = city.Name }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<CityDto>.Failure(CityErrors.NotFound);
        }

        return Result<CityDto>.Success(dto);
    }
}
