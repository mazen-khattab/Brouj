using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Commands.AddStageItem;
using Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;
using Brouj.Application.Features.TimePlans.Commands.DeleteStageItem;
using Brouj.Application.Features.TimePlans.Commands.ReorderStageItems;
using Brouj.Application.Features.TimePlans.Commands.UpdateStageItem;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/time-plan-stages")]
public sealed class AdminTimePlanStagesController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminTimePlanStagesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{stageId}/items")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(AddPlanStageItemCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> AddPlanStageItem(
        [FromRoute] Guid stageId,
        [FromBody] PlanStageItemInput body,
        CancellationToken cancellationToken)
    {
        var request = new AddPlanStageItemCommand
        {
            StageId = stageId,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{stageId}/items/{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdatePlanStageItemCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdatePlanStageItem(
        [FromRoute] Guid stageId,
        [FromRoute] Guid id,
        [FromBody] PlanStageItemInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdatePlanStageItemCommand
        {
            StageId = stageId,
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{stageId}/items/{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeletePlanStageItemCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeletePlanStageItem(
        [FromRoute] Guid stageId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var request = new DeletePlanStageItemCommand
        {
            StageId = stageId,
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{stageId}/items/{id}/status")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ChangePlanStageItemStatusCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ChangePlanStageItemStatus(
        [FromRoute] Guid stageId,
        [FromRoute] Guid id,
        [FromBody] ChangePlanStageItemStatusRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ChangePlanStageItemStatusCommand
        {
            StageId = stageId,
            Id = id,
            Status = body.Status,
            ConfirmedDate = body.ConfirmedDate
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{stageId}/items/order")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReorderPlanStageItemsCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReorderPlanStageItems(
        [FromRoute] Guid stageId,
        [FromBody] ReorderPlanStageItemsRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReorderPlanStageItemsCommand
        {
            StageId = stageId,
            Items = body.Items
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
