using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.Commands.Create;
using Brouj.Application.Features.Users.Commands.Restore;
using Brouj.Application.Features.Users.Commands.SoftDelete;
using Brouj.Application.Features.Users.Commands.Update;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.Features.Users.Queries.GetAll;
using Brouj.Application.Features.Users.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminUsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetUsersQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<UserDto>>), 200)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersParameters query, CancellationToken cancellationToken)
    {
        var request = new GetUsersQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search,
            IsDeleted = query.IsDeleted,
            Role = query.Role
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetUserByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetUserByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(CreateUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserInput body, CancellationToken cancellationToken)
    {
        var request = new CreateUserCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(UpdateUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateUserInput body,
        CancellationToken cancellationToken)
    {
        var request = new UpdateUserCommand
        {
            Id = id,
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(SoftDeleteUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> SoftDeleteUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new SoftDeleteUserCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.SuperAdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(RestoreUserCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> RestoreUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new RestoreUserCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
