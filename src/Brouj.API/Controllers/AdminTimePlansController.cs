using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Commands.AddStage;
using Brouj.Application.Features.TimePlans.Commands.AddStageFromTemplate;
using Brouj.Application.Features.TimePlans.Commands.Delete;
using Brouj.Application.Features.TimePlans.Commands.DeleteStage;
using Brouj.Application.Features.TimePlans.Commands.ReorderStages;
using Brouj.Application.Features.TimePlans.Commands.Update;
using Brouj.Application.Features.TimePlans.Commands.UpdateStage;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/time-plans")]
public sealed class AdminTimePlansController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminTimePlansController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateTimePlanCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateTimePlan(
        [FromRoute] Guid id,
        [FromBody] TimePlanInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateTimePlanCommand
        {
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteTimePlanCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteTimePlan([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteTimePlanCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{timePlanId}/stages")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(AddTimePlanStageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> AddTimePlanStage(
        [FromRoute] Guid timePlanId,
        [FromBody] StageDraftInput body,
        CancellationToken cancellationToken)
    {
        var request = new AddTimePlanStageCommand
        {
            TimePlanId = timePlanId,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPost("{timePlanId}/stages/from-template")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(AddStageFromTemplateCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> AddStageFromTemplate(
        [FromRoute] Guid timePlanId,
        [FromBody] StageDraftInput body,
        CancellationToken cancellationToken)
    {
        var request = new AddStageFromTemplateCommand
        {
            TimePlanId = timePlanId,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{timePlanId}/stages/{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateTimePlanStageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateTimePlanStage(
        [FromRoute] Guid timePlanId,
        [FromRoute] Guid id,
        [FromBody] TimePlanInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateTimePlanStageCommand
        {
            TimePlanId = timePlanId,
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{timePlanId}/stages/{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteTimePlanStageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteTimePlanStage(
        [FromRoute] Guid timePlanId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeleteTimePlanStageCommand
        {
            TimePlanId = timePlanId,
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{timePlanId}/stages/order")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReorderTimePlanStagesCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReorderTimePlanStages(
        [FromRoute] Guid timePlanId,
        [FromBody] ReorderTimePlanStagesRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReorderTimePlanStagesCommand
        {
            TimePlanId = timePlanId,
            Items = body.Items
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
