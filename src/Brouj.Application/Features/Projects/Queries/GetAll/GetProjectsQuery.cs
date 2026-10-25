using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Projects.Queries.GetAll;

public sealed class GetProjectsQuery : IRequest<Result<PaginationResult<ProjectListDto>>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public Guid? CityId { get; init; }

    public Guid? AreaId { get; init; }

    public ProjectType? ProjectType { get; init; }
}
