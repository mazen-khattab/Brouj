using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Cities.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Cities.Queries.GetAll;

public sealed class GetCitiesQueryHandler : IRequestHandler<GetCitiesQuery, Result<PaginationResult<CityDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetCitiesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<CityDto>>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(city => city.Name.Contains(request.Search.Trim()));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(city => city.Name).ThenBy(city => city.Id).Select(city => new CityDto { Id = city.Id, Name = city.Name }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<CityDto>>.Success(page);
    }
}
