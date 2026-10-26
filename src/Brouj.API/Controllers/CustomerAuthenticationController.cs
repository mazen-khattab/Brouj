using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.Commands.Login;
using Brouj.Application.Features.Customers.Commands.Logout;
using Brouj.Application.Features.Customers.Commands.RefreshSession;
using Brouj.Application.Features.Customers.Commands.Register;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/auth/customers")]
public sealed class CustomerAuthenticationController : ApiControllerBase
{
    private readonly ISender _sender;

    public CustomerAuthenticationController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(RegisterCustomerCommand))]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionDto>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerInput body,
        CancellationToken cancellationToken)
    {
        var request = new RegisterCustomerCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result, 201);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(LoginCustomerCommand))]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionDto>), 200)]
    [Consumes("application/json")]
    public async Task<IActionResult> LoginCustomer([FromBody] LoginInput body, CancellationToken cancellationToken)
    {
        var request = new LoginCustomerCommand
        {
            Input = body
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(RefreshCustomerSessionCommand))]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionDto>), 200)]
    public async Task<IActionResult> RefreshCustomerSession(CancellationToken cancellationToken)
    {
        var request = new RefreshCustomerSessionCommand();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.Authentication)]
    [EndpointContract(typeof(LogoutCustomerCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> LogoutCustomer(CancellationToken cancellationToken)
    {
        var request = new LogoutCustomerCommand();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
