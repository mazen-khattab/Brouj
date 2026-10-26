using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.Commands.Restore;
using Brouj.Application.Features.Customers.Commands.SoftDelete;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.Customers.Queries.GetAll;
using Brouj.Application.Features.Customers.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/customers")]
public sealed class AdminCustomersController : ApiControllerBase
{
    private readonly ISender _sender;

    public AdminCustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetCustomersQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<CustomerDto>>), 200)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] GetCustomersParameters query,
        CancellationToken cancellationToken)
    {
        var request = new GetCustomersQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Search = query.Search,
            IsDeleted = query.IsDeleted
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetCustomerByIdQuery))]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    public async Task<IActionResult> GetCustomerById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new GetCustomerByIdQuery
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(SoftDeleteCustomerCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> SoftDeleteCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new SoftDeleteCustomerCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.AdminWrites)]
    [EndpointContract(typeof(RestoreCustomerCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 200)]
    public async Task<IActionResult> RestoreCustomer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = new RestoreCustomerCommand
        {
            Id = id
        };
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
