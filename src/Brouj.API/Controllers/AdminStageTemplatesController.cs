using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.Commands.Create;
using Brouj.Application.Features.StageTemplates.Commands.Delete;
using Brouj.Application.Features.StageTemplates.Commands.SetActive;
using Brouj.Application.Features.StageTemplates.Commands.SetItems;
using Brouj.Application.Features.StageTemplates.Commands.Update;
using Brouj.Application.Features.StageTemplates.DTOs;
using Brouj.Application.Features.StageTemplates.Queries.GetAll;
using Brouj.Application.Features.StageTemplates.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/stage-templates")]
public sealed class AdminStageTemplatesController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminStageTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetStageTemplatesQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<StageTemplateListDto>>), 200)]
    public async Task<IActionResult> GetStageTemplates(
        [FromQuery] GetStageTemplatesParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetStageTemplatesQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search,
            IsActive = query.IsActive
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetStageTemplateByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<StageTemplateDto>), 200)]
    public async Task<IActionResult> GetStageTemplateById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetStageTemplateByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateStageTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateStageTemplate(
        [FromBody] StageTemplateInput body,
        CancellationToken cancellationToken)
    {
        var request = new CreateStageTemplateCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateStageTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateStageTemplate(
        [FromRoute] Guid id,
        [FromBody] StageTemplateInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateStageTemplateCommand
        {
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{id}/active")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(SetStageTemplateActiveCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> SetStageTemplateActive(
        [FromRoute] Guid id,
        [FromBody] SetStageTemplateActiveRequest body,
        CancellationToken cancellationToken)
    {
        var request = new SetStageTemplateActiveCommand
        {
            Id = id,
            IsActive = body.IsActive
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteStageTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteStageTemplate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteStageTemplateCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{id}/items")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(SetStageTemplateItemsCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> SetStageTemplateItems(
        [FromRoute] Guid id,
        [FromBody] SetStageTemplateItemsRequest body,
        CancellationToken cancellationToken)
    {
        var request = new SetStageTemplateItemsCommand
        {
            Id = id,
            Items = body.Items
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
