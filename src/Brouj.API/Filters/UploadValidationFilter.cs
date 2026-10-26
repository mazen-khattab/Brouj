using Brouj.API.Extensions;
using Brouj.Application.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Brouj.API.Filters;

/// <summary>Bounds individual files after multipart binding; the storage adapter validates the actual format.</summary>
public sealed class UploadValidationFilter : IAsyncActionFilter
{
    private readonly FileStorageOptions _options;

    public UploadValidationFilter(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var policy = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

        if (policy != RateLimitCategories.FileUploads)
        {
            await next();
            return;
        }

        var files = context.HttpContext.Request.Form.Files;
        var maximum = Math.Min(_options.MaxFileSizeBytes, 5 * 1024 * 1024);
        var status = files.Any(file => file.Length > maximum) ? 413
            : files.Count != 1 || files[0].Length == 0 ? 400 : 0;

        if (status != 0)
        {
            context.Result = new ObjectResult(SafeProblems.Create(context.HttpContext, status)) { StatusCode = status };
            return;
        }

        await next();
    }
}
