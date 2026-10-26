using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.Commands.AddImage;
using Brouj.Application.Features.Units.Commands.Delete;
using Brouj.Application.Features.Units.Commands.DeleteImage;
using Brouj.Application.Features.Units.Commands.ReplaceAmenities;
using Brouj.Application.Features.Units.Commands.Update;
using Brouj.Application.Features.Units.Commands.UpdateImage;
using Brouj.Application.Features.Units.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/units")]
public sealed class AdminUnitsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminUnitsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateUnitCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateUnit(
        [FromRoute] Guid id,
        [FromQuery] UpdateUnitParameters query,
        [FromBody] UnitInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateUnitCommand
        {
            Id = id,
            ProjectId = query.ProjectId,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteUnitCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteUnit(
        [FromRoute] Guid id,
        [FromQuery] DeleteUnitParameters query,
        CancellationToken cancellationToken)
    {
        var request = new DeleteUnitCommand
        {
            Id = id,
            ProjectId = query.ProjectId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{id}/amenities")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReplaceUnitAmenitiesCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReplaceUnitAmenities(
        [FromRoute] Guid id,
        [FromBody] ReplaceUnitAmenitiesRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReplaceUnitAmenitiesCommand
        {
            UnitId = id,
            Amenities = body.Amenities
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/images")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.FileUploads)]
    [EndpointContract(typeof(AddUnitImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddUnitImage(
        [FromRoute] Guid id,
        [FromForm] AddUnitImageRequest body,
        CancellationToken cancellationToken)
    {
        // The transport owns the stream; the storage service validates names, signatures and actual byte counts.
        await using var stream = body.File.OpenReadStream();

        var request = new AddUnitImageCommand
        {
            UnitId = id,
            File = new UploadInput
            {
                Content = stream,
                FileName = body.File.FileName,
                ContentType = body.File.ContentType,
                Length = body.File.Length
            },
            AltText = body.AltText
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}/images/{imageId}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateUnitImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateUnitImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        [FromBody] UpdateUnitImageRequest body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateUnitImageCommand
        {
            UnitId = id,
            Id = imageId,
            AltText = body.AltText
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteUnitImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteUnitImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var request = new DeleteUnitImageCommand
        {
            UnitId = id,
            Id = imageId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
