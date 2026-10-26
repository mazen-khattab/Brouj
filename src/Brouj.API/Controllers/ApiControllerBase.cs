using Brouj.API.Extensions;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Brouj.API.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Respond<T>(Result<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatus, new ApiResponse<T>(result.Value));
        }

        var status = result.Error.Type switch
        {
            ErrorType.Validation => 400,
            ErrorType.Unauthorized => 401,
            ErrorType.Forbidden => 403,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            _ => 500
        };

        if (status == 401)
        {
            Response.Headers.WWWAuthenticate = "Bearer";
        }

        HttpContext.RequestServices.GetRequiredService<ILogger<ApiControllerBase>>()
            .LogWarning("Application error {ErrorCode} mapped to HTTP {StatusCode}; trace {TraceId}",
                result.Error.Code, status, HttpContext.TraceIdentifier);

        // Failure messages from providers/exceptions never enter this explicit Application error contract.
        return status == 500 ? StatusCode(status, SafeProblems.Create(HttpContext, status))
            : StatusCode(status, new ApiErrorResponse(result.Error.Code, result.Error.Message));
    }
}
