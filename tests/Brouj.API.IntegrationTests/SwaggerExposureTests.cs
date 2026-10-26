using System.Net;
using System.Text.Json;
using Brouj.API.IntegrationTests.Support;
using Microsoft.Extensions.Hosting;

namespace Brouj.API.IntegrationTests;

/// <summary>Exercises the real documentation middleware and CSP boundaries without SQL Server.</summary>
public sealed class SwaggerExposureTests
{
    [Fact]
    public async Task Development_serves_Swagger_HTML_assets_and_schema_with_a_scoped_CSP_exception()
    {
        using var factory = new Part8ApiFactory(true, new()
        {
            [HostDefaults.EnvironmentKey] = Environments.Development
        });
        using var client = factory.CreateHttpsClient();
        using var page = await client.GetAsync("/index.html");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType!.MediaType);
        Assert.Contains("Swagger UI", await page.Content.ReadAsStringAsync());
        var policy = Assert.Single(page.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self' 'unsafe-inline'", policy);
        Assert.Contains("style-src 'self' 'unsafe-inline'", policy);
        Assert.Contains("connect-src 'self'", policy);
        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.Equal("no-store", page.Headers.CacheControl!.ToString());

        foreach (var path in new[] { "/swagger-ui.css", "/swagger-ui-bundle.js", "/swagger-ui-standalone-preset.js" })
        {
            using var asset = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
        }

        using var schema = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, schema.StatusCode);
        using var document = JsonDocument.Parse(await schema.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/v1/cities", out _));

        using var api = await client.GetAsync("/health/live");
        Assert.Equal("default-src 'none'; frame-ancestors 'none'",
            Assert.Single(api.Headers.GetValues("Content-Security-Policy")));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger-ui.css")]
    [InlineData("/swagger-ui-bundle.js")]
    [InlineData("/openapi/v1.json")]
    public async Task Production_exposes_no_documentation_and_keeps_the_restrictive_CSP(string path)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("default-src 'none'; frame-ancestors 'none'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")));
    }
}
