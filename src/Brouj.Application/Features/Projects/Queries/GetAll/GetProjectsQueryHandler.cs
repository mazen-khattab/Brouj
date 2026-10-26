using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Projects.Queries.GetAll;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<PaginationResult<ProjectListDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetProjectsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<ProjectListDto>>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.BaseProjects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(project => project.Name.Contains(request.Search.Trim()));
        }

        if (request.CityId is not null)
        {
            query = query.Where(project => project.Area.CityId == request.CityId);
        }

        if (request.AreaId is not null)
        {
            query = query.Where(project => project.AreaId == request.AreaId);
        }

        if (request.ProjectType is not null)
        {
            query = query.Where(project => project.ProjectType == request.ProjectType);
        }

        var page = await Paging.ToPageAsync(query.OrderBy(project => project.Name).ThenBy(project => project.Id).Select(project => new ProjectListDto
        {
            Id = project.Id,
            Name = project.Name,
            AreaId = project.AreaId,
            AreaName = project.Area.Name,
            CityId = project.Area.CityId,
            ProjectType = project.ProjectType,
            Price = project.Price,
            Size = project.Size
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<ProjectListDto>>.Success(page);
    }
}
