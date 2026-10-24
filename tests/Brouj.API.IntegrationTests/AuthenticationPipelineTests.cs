using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Brouj.API.IntegrationTests;

/// <summary>Exercises the real Program pipeline without SQL Server, Docker, or production credentials.</summary>
public sealed class AuthenticationPipelineTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Cookie_authentication_establishes_the_current_customer_before_the_endpoint_runs()
    {
        using var client = factory.CreateHttpsClient();
        var (customerId, accessCookie) = CustomerCredential();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/__tests/pipeline/identity");
        request.Headers.Add("Cookie", accessCookie);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var identity = await response.Content.ReadFromJsonAsync<IdentityResponse>();
        Assert.Equal(customerId, identity!.CustomerId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_CSRF_proof_is_rejected_before_authorization(bool authenticated)
    {
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/__tests/pipeline/identity");
        if (authenticated) request.Headers.Add("Cookie", CustomerCredential().Cookie);

        using var response = await client.SendAsync(request);

        // The anonymous case would return 401 if authorization ran before cookie protection.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_CSRF_proof_is_validated_after_authentication()
    {
        using var client = factory.CreateHttpsClient();
        var (customerId, accessCookie) = CustomerCredential();
        using var bootstrapRequest = new HttpRequestMessage(HttpMethod.Get, "/__tests/pipeline/csrf");
        bootstrapRequest.Headers.Add("Cookie", accessCookie);
        using var bootstrapResponse = await client.SendAsync(bootstrapRequest);
        Assert.Equal(HttpStatusCode.OK, bootstrapResponse.StatusCode);
        var csrf = await bootstrapResponse.Content.ReadFromJsonAsync<CsrfResponse>();
        var csrfCookie = Assert.Single(bootstrapResponse.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("__Host-brouj-csrf=", StringComparison.Ordinal)).Split(';')[0];
        using var request = new HttpRequestMessage(HttpMethod.Post, "/__tests/pipeline/identity");
        request.Headers.Add("Cookie", accessCookie + "; " + csrfCookie);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.RequestToken);

        using var response = await client.SendAsync(request);

        // This proof was issued for the authenticated customer. Validation before authentication
        // would see an anonymous identity and reject it, so success checks the actual middleware order.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var identity = await response.Content.ReadFromJsonAsync<IdentityResponse>();
        Assert.Equal(customerId, identity!.CustomerId);
    }

    [Fact]
    public async Task Safe_requests_still_require_authorization_for_protected_endpoints()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/__tests/pipeline/identity");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Production_errors_return_problem_details_without_internal_exception_details()
    {
        using var client = factory.CreateHttpsClient();

        using var response = await client.GetAsync("/__tests/pipeline/failure");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(PipelineProbeController.InternalError, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    private (Guid CustomerId, string Cookie) CustomerCredential()
    {
        var customerId = Guid.NewGuid();
        var jwt = factory.Services.GetRequiredService<IJwtTokenService>();
        return (customerId, "__Host-brouj-access=" + jwt.GenerateCustomerAccessToken(customerId));
    }

    private sealed record IdentityResponse(Guid? CustomerId);
    private sealed record CsrfResponse(string RequestToken);
}

/// <summary>Starts the actual API entry point with isolated, synthetic test configuration.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Program reads its connection string before Build. Host configuration reaches the entry
        // point early enough for that read; late web-host configuration would miss it.
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [HostDefaults.EnvironmentKey] = Environments.Production,
                // No test endpoint queries SQL; this supplies the existing persistence registration only.
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=BroujApiPipelineTests;Integrated Security=True;TrustServerCertificate=True;",
                ["Jwt:Issuer"] = "brouj-pipeline-tests",
                ["Jwt:Audience"] = "brouj-pipeline-test-client",
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ["FileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "BroujApiPipelineTests", Guid.NewGuid().ToString("N")),
                ["Caching:Provider"] = "Memory"
            }));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureTestServices(services =>
        {
            // Test controllers exist only in this test host; production discovers only API controllers.
            services.AddControllers().AddApplicationPart(typeof(PipelineProbeController).Assembly);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://brouj.test"), AllowAutoRedirect = false, HandleCookies = false
    });
}

/// <summary>Test-host-only endpoints that expose observable authentication, CSRF, and error-handler behavior.</summary>
[ApiController]
[Route("__tests/pipeline")]
public sealed class PipelineProbeController(ICurrentUser actor, AuthenticationCookies cookies) : ControllerBase
{
    internal const string InternalError = "Synthetic internal exception that must not reach the client.";

    [Authorize(Policy = "Customer")]
    [HttpGet("identity")]
    public IActionResult GetIdentity() => Ok(new { actor.CustomerId });

    [Authorize(Policy = "Customer")]
    [HttpPost("identity")]
    public IActionResult PostIdentity() => Ok(new { actor.CustomerId });

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrf() => Ok(new { RequestToken = cookies.GetCsrfToken(HttpContext) });

    [AllowAnonymous]
    [HttpGet("failure")]
    public IActionResult Fail() => throw new InvalidOperationException(InternalError);
}
