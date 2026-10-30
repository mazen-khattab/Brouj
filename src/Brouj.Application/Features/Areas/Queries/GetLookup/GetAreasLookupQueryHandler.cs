using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Areas.Queries.GetLookup;

/// <summary>
/// Handler for processing area lookup queries with read-only database projections.
/// </summary>
public sealed class GetAreasLookupQueryHandler : IRequestHandler<GetAreasLookupQuery, Result<IEnumerable<LookupDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetAreasLookupQueryHandler> _logger;

    public GetAreasLookupQueryHandler(IApplicationDbContext db, ILogger<GetAreasLookupQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<LookupDto>>> Handle(
        GetAreasLookupQuery request,
        CancellationToken cancellationToken)
    {
        // Structured logging of lookup initiation with filter parameters
        _logger.LogInformation("Initiating areas lookup with search term: {SearchTerm}", request.Search);

        // Performance: query database with AsNoTracking() for read-only optimization
        var query = _db.Areas.AsNoTracking();

        // Filtering: apply Where clause if search string is not null or whitespace
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            query = query.Where(area => area.Name.Contains(searchTerm));
        }

        // Projection: project data directly at database level via Select() before execution
        // to avoid fetching entire entities into memory
        var items = await query
            .OrderBy(area => area.Name)
            .ThenBy(area => area.Id)
            .Select(area => new LookupDto
            {
                Id = area.Id,
                Name = area.Name
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully completed areas lookup, returning {Count} items", items.Count);

        return Result<IEnumerable<LookupDto>>.Success(items);
    }
}
