using Brouj.API.Extensions;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ContactSubmissions.Commands.Submit;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Brouj.API.Controllers;

[Route("api/v1/contact-submissions")]
public sealed class ContactSubmissionsController(ISender sender) : ApiControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitCategories.ContactSubmissions)]
    [EndpointContract(typeof(SubmitContactSubmissionCommand))]
    [ProducesResponseType(typeof(ApiResponse<Guid>), 201)]
    [Consumes("application/json")]
    public async Task<IActionResult> Submit([FromBody] ContactSubmissionInput body, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitContactSubmissionCommand { Input = body }, cancellationToken);
        return Respond(result, 201);
    }
}
