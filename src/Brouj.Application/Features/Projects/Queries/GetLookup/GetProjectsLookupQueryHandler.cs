using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Projects.Queries.GetLookup;

/// <summary>
/// Handler for processing project lookup queries with read-only database projections.
/// </summary>
public sealed class GetProjectsLookupQueryHandler : IRequestHandler<GetProjectsLookupQuery, Result<IEnumerable<LookupDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<GetProjectsLookupQueryHandler> _logger;

    public GetProjectsLookupQueryHandler(IApplicationDbContext db, ILogger<GetProjectsLookupQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<LookupDto>>> Handle(
        GetProjectsLookupQuery request,
        CancellationToken cancellationToken)
    {
        // Structured logging of lookup initiation with filter parameters
        _logger.LogInformation(
            "Initiating projects lookup with search term: {SearchTerm}, Type: {ProjectType}",
            request.Search,
            request.Type);

        // Performance: query database with AsNoTracking() for read-only optimization
        var query = _db.BaseProjects.AsNoTracking();

        // Filtering: apply Where clause if search string is not null or whitespace
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            query = query.Where(project => project.Name.Contains(searchTerm));
        }

        // Filtering: filter by ProjectType if provided
        if (request.Type is not null)
        {
            query = query.Where(project => project.ProjectType == request.Type);
        }

        // Projection: project data directly at database level via Select() before execution
        // to avoid fetching entire entities into memory
        var items = await query
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Select(project => new LookupDto
            {
                Id = project.Id,
                Name = project.Name
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Successfully completed projects lookup, returning {Count} items", items.Count);

        return Result<IEnumerable<LookupDto>>.Success(items);
    }
}
