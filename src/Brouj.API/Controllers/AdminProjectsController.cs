using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.Commands.AddImage;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.Commands.Delete;
using Brouj.Application.Features.Projects.Commands.DeleteImage;
using Brouj.Application.Features.Projects.Commands.ReplaceAmenities;
using Brouj.Application.Features.Projects.Commands.Update;
using Brouj.Application.Features.Projects.Commands.UpdateImage;
using Brouj.Application.Features.Projects.DTOs;
using Brouj.Application.Features.TimePlans.Commands.Create;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.Units.Commands.Create;
using Brouj.Application.Features.Units.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/projects")]
public sealed class AdminProjectsController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminProjectsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateProjectCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectInput body, CancellationToken cancellationToken)
    {
        var request = new CreateProjectCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateProjectCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateProject(
        [FromRoute] Guid id,
        [FromBody] ProjectInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateProjectCommand
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
    [EndpointContract(typeof(DeleteProjectCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteProject([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new DeleteProjectCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPut("{id}/amenities")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(ReplaceProjectAmenitiesCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> ReplaceProjectAmenities(
        [FromRoute] Guid id,
        [FromBody] ReplaceProjectAmenitiesRequest body,
        CancellationToken cancellationToken)
    {
        var request = new ReplaceProjectAmenitiesCommand
        {
            ProjectId = id,
            Amenities = body.Amenities
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/images")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.FileUploads)]
    [EndpointContract(typeof(AddProjectImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddProjectImage(
        [FromRoute] Guid id,
        [FromForm] AddProjectImageRequest body,
        CancellationToken cancellationToken)
    {
        // The transport owns the stream; the storage service validates names, signatures and actual byte counts.
        await using var stream = body.File.OpenReadStream();

        var request = new AddProjectImageCommand
        {
            ProjectId = id,
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
    [EndpointContract(typeof(UpdateProjectImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateProjectImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        [FromBody] UpdateProjectImageRequest body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateProjectImageCommand
        {
            ProjectId = id,
            Id = imageId,
            AltText = body.AltText
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(DeleteProjectImageCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> DeleteProjectImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        var request = new DeleteProjectImageCommand
        {
            ProjectId = id,
            Id = imageId
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{projectId}/units")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateUnitCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateUnit(
        [FromRoute] Guid projectId,
        [FromBody] UnitInput body,
        CancellationToken cancellationToken)
    {
        var request = new CreateUnitCommand
        {
            ProjectId = projectId,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPost("{projectId}/time-plan")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateTimePlanCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateTimePlan(
        [FromRoute] Guid projectId,
        [FromBody] CreateTimePlanRequest body,
        CancellationToken cancellationToken)
    {
        var request = new CreateTimePlanCommand
        {
            ProjectId = projectId,
            Input = body.Input,
            Stages = body.Stages
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }
}
