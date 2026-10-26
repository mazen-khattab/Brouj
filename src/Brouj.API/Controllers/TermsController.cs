using Brouj.API.Authorization;
using Brouj.API.Extensions;
using Brouj.API.Requests;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.Terms.Queries.GetActive;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/terms-and-conditions")]
public sealed class TermsController : ApiControllerBase
{
    private readonly ISender _sender;

    public TermsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("current")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.PublicReads)]
    [EndpointContract(typeof(GetActiveTermsAndConditionsQuery))]
    [ProducesResponseType(typeof(ApiResponse<TermsDto>), 200)]
    public async Task<IActionResult> GetActiveTermsAndConditions(CancellationToken cancellationToken)
    {
        var request = new GetActiveTermsAndConditionsQuery();
        var result = await _sender.Send(request, cancellationToken);

        return Respond(result);
    }
}
