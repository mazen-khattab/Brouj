using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Queries.GetByCity;

public sealed class GetAreasByCityQueryHandler : IRequestHandler<GetAreasByCityQuery, Result<PaginationResult<AreaListDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetAreasByCityQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<AreaListDto>>> Handle(
        GetAreasByCityQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Areas.AsNoTracking();

        query = query.Where(area => area.CityId == request.CityId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(area => area.Name.Contains(request.Search.Trim()));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(area => area.Name).ThenBy(area => area.Id).Select(area => new AreaListDto { Id = area.Id, Name = area.Name, CityId = area.CityId, CityName = area.City.Name }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<AreaListDto>>.Success(page);
    }
}
