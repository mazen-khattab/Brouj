using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Commands.AddStageItemImage;
using Brouj.Application.Features.TimePlans.Commands.DeleteStageItemImage;
using Brouj.Application.Features.TimePlans.Commands.ReorderStageItemImages;
using Brouj.Application.Features.TimePlans.Commands.UpdateStageItemImage;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/time-plan-items")]
public sealed class AdminTimePlanItemsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminTimePlanItemsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{itemId}/images")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.FileUploads)]
    [EndpointContract(typeof(AddStageItemImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddStageItemImage(
        [FromRoute] Guid itemId,
        [FromForm] AddStageItemImageRequest body,
        CancellationToken cancellationToken)
    {
        // The transport owns the stream; the storage service validates names, signatures and actual byte counts.
        await using var stream = body.File.OpenReadStream();

        var request = new AddStageItemImageCommand
        {
            ItemId = itemId,
            File = new UploadInput
            {
                Content = stream,
                FileName = body.File.FileName,
                ContentType = body.File.ContentType,
                Length = body.File.Length
            },
            Description = body.Description
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{itemId}/images/{imageId}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateStageItemImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateStageItemImage(
        [FromRoute] Guid itemId,
        [FromRoute] Guid imageId,
        [FromBody] UpdateStageItemImageRequest body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateStageItemImageCommand
        {
            ItemId = itemId,
            Id = imageId,
            Description = body.Description
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{itemId}/images/{imageId}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteStageItemImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteStageItemImage(
        [FromRoute] Guid itemId,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var request = new DeleteStageItemImageCommand
        {
            ItemId = itemId,
            Id = imageId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{itemId}/images/order")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReorderStageItemImagesCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReorderStageItemImages(
        [FromRoute] Guid itemId,
        [FromBody] ReorderStageItemImagesRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReorderStageItemImagesCommand
        {
            ItemId = itemId,
            Items = body.Items
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
