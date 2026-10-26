using Brouj.API.Configuration;
using Brouj.API.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Brouj.API.Middleware;

public sealed class RequestLimitsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiOptions _options;

    public RequestLimitsMiddleware(RequestDelegate next, IOptions<ApiOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var upload = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
            == RateLimitCategories.FileUploads;
        var limit = upload ? _options.MultipartBodyLimitBytes : _options.JsonBodyLimitBytes;
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();

        if (feature is { IsReadOnly: false })
        {
            feature.MaxRequestBodySize = limit;
        }

        if (context.Request.ContentLength > limit)
        {
            await SafeProblems.WriteAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }

        // Enforce streamed/chunked limits too, including hosts whose body-size feature is absent.
        context.Request.Body = new LimitedRequestStream(context.Request.Body, limit);

        await _next(context);
    }
}
