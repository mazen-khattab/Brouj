using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Cities.Queries.GetLookup;

/// <summary>
/// Handler for processing city lookup queries with read-only database projections.
/// </summary>
public sealed class GetCitiesLookupQueryHandler : IRequestHandler<GetCitiesLookupQuery, Result<IEnumerable<LookupDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetCitiesLookupQueryHandler> _logger;

    public GetCitiesLookupQueryHandler(IApplicationDbContext db, ILogger<GetCitiesLookupQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<LookupDto>>> Handle(
        GetCitiesLookupQuery request,
        CancellationToken cancellationToken)
    {
        // Structured logging of lookup initiation with filter parameters
        _logger.LogInformation("Initiating cities lookup with search term: {SearchTerm}", request.Search);

        // Performance: query database with AsNoTracking() for read-only optimization
        var query = _db.Cities.AsNoTracking();

        // Filtering: apply Where clause if search string is not null or whitespace
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            query = query.Where(city => city.Name.Contains(searchTerm));
        }

        // Projection: project data directly at database level via Select() before execution
        // to avoid fetching entire entities into memory
        var items = await query
            .OrderBy(city => city.Name)
            .ThenBy(city => city.Id)
            .Select(city => new LookupDto
            {
                Id = city.Id,
                Name = city.Name
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully completed cities lookup, returning {Count} items", items.Count);

        return Result<IEnumerable<LookupDto>>.Success(items);
    }
}
