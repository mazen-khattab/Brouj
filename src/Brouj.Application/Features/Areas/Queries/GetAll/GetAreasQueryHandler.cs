using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Areas.Queries.GetAll;

public sealed class GetAreasQueryHandler : IRequestHandler<GetAreasQuery, Result<PaginationResult<AreaListDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetAreasQueryHandler> _logger;

    public GetAreasQueryHandler(IApplicationDbContext db, ILogger<GetAreasQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PaginationResult<AreaListDto>>> Handle(GetAreasQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching areas page {PageNumber} with size {PageSize}. Search term: {SearchTerm}", request.PageNumber, request.PageSize, request.Search);

        var query = _db.Areas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(area => area.Name.Contains(request.Search.Trim()));
        }

        var page = await Paging.ToPageAsync(query
            .OrderBy(area => area.Name)
            .ThenBy(area => area.Id)
            .Select(area => new AreaListDto
            {
                Id = area.Id,
                Name = area.Name,
                CityId = area.CityId,
                CityName = area.City.Name
            }),
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation("Successfully retrieved areas page {PageNumber}.", request.PageNumber);

        return Result<PaginationResult<AreaListDto>>.Success(page);
    }
}