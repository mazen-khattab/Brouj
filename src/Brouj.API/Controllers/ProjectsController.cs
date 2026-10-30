using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.Projects.Queries.GetAll;
using Brouj.Application.Features.Projects.Queries.GetById;
using Brouj.Application.Features.Projects.Queries.GetLookup;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.TimePlans.Queries.GetByProject;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Queries.GetByProject;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/projects")]
public sealed class ProjectsController : ApiControllerBase
{
    private readonly ISender _sender;

    public ProjectsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetProjectsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ProjectListDto>>), 200)]
    public async Task<IActionResult> GetProjects([FromQuery] GetProjectsParameters query, CancellationToken cancellationToken)
    {
        var request = new GetProjectsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search,
            CityId = query.CityId,
            AreaId = query.AreaId,
            ProjectType = query.ProjectType
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    /// <summary>
    /// Retrieves a lightweight, read-only lookup list of projects with direct database projection, optionally filtered by name and project type.
    /// </summary>
    /// <param name="query">Optional filtering parameters including search term and project type.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of project lookup items containing identifier and name.</returns>
    [HttpGet("lookup")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetProjectsLookupQuery))]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<LookupDto>>), 200)]
    public async Task<IActionResult> GetProjectsLookup([FromQuery] GetProjectsLookupParameters query, CancellationToken cancellationToken)
    {
        var request = new GetProjectsLookupQuery(query.Search, query.Type);
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetProjectByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    public async Task<IActionResult> GetProjectById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetProjectByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{projectId}/units")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetUnitsByProjectQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UnitListDto>>), 200)]
    public async Task<IActionResult> GetUnitsByProject([FromRoute] Guid projectId, [FromQuery] GetUnitsByProjectParameters query, CancellationToken cancellationToken)
    {
        var request = new GetUnitsByProjectQuery
        {
            ProjectId = projectId,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{projectId}/time-plan")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetProjectTimePlanQuery))]
    [ProducesResponseType(typeof(ApiResponse<TimePlanDto>), 200)]
    public async Task<IActionResult> GetProjectTimePlan([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var request = new GetProjectTimePlanQuery
        {
            ProjectId = projectId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
