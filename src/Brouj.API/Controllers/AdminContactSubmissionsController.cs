using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using Brouj.Application.Features.ContactSubmissions.Queries.GetAll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/admin/contact-submissions")]
public sealed class AdminContactSubmissionsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetContactSubmissionsQuery))]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ContactSubmissionDto>>), 200)]
    public async Task<IActionResult> Get([FromQuery] PaginationParameters query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetContactSubmissionsQuery
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        }, cancellationToken);

        return Respond(result);
    }
}
