using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.Commands.Create;
using Brouj.Application.Features.ItemTemplates.Commands.Delete;
using Brouj.Application.Features.ItemTemplates.Commands.SetActive;
using Brouj.Application.Features.ItemTemplates.Commands.Update;
using Brouj.Application.Features.ItemTemplates.DTOs;
using Brouj.Application.Features.ItemTemplates.Queries.GetAll;
using Brouj.Application.Features.ItemTemplates.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/item-templates")]
public sealed class AdminItemTemplatesController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminItemTemplatesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetItemTemplatesQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ItemTemplateListDto>>), 200)]
    public async Task<IActionResult> GetItemTemplates(
        [FromQuery] GetItemTemplatesParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetItemTemplatesQuery
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
    [EndpointContract(typeof(GetItemTemplateByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<ItemTemplateDto>), 200)]
    public async Task<IActionResult> GetItemTemplateById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetItemTemplateByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateItemTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateItemTemplate(
        [FromBody] ItemTemplateInput body,
        CancellationToken cancellationToken)
    {
        var request = new CreateItemTemplateCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateItemTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateItemTemplate(
        [FromRoute] Guid id,
        [FromBody] ItemTemplateInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateItemTemplateCommand
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
    [EndpointContract(typeof(SetItemTemplateActiveCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> SetItemTemplateActive(
        [FromRoute] Guid id,
        [FromBody] SetItemTemplateActiveRequest body,
        CancellationToken cancellationToken)
    {
        var request = new SetItemTemplateActiveCommand
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
    [EndpointContract(typeof(DeleteItemTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteItemTemplate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteItemTemplateCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
