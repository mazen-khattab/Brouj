using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Brouj.API.Extensions;
using Brouj.API.IntegrationTests.Support;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.API.IntegrationTests;

public sealed class ApiHardeningTests
{
    [Theory]
    [InlineData("https://brouj.com", true)]
    [InlineData("https://evil.example", false)]
    [InlineData("https://brouj.com.evil.example", false)]
    [InlineData("http://brouj.com", false)]
    [InlineData("https://localhost:5173", false)]
    public async Task CORS_allows_only_exact_configured_origins_with_credentials(string origin, bool allowed)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/customers/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));

        if (allowed)
        {
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        }
    }

    [Fact]
    public async Task Configured_local_HTTPS_origin_is_supported()
    {
        using var factory = new Part8ApiFactory(true, new() { ["Api:AllowedOrigins:0"] = "https://localhost:5173" });
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf");
        request.Headers.Add("Origin", "https://localhost:5173");

        using var response = await client.SendAsync(request);

        Assert.Equal("https://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Theory]
    [InlineData("PublicReads", "GetCitiesQuery")]
    [InlineData("Authentication", null)]
    [InlineData("ReservationWrites", "CreateReservationCommand")]
    [InlineData("AdminWrites", "CreateCityCommand")]
    [InlineData("FileUploads", "AddProjectImageCommand")]
    public async Task Each_rate_category_rejects_excess_requests_with_429(string category, string? requestName)
    {
        using var factory = new Part8ApiFactory(true, new() { [$"RateLimits:{category}:PermitLimit"] = "2" });
        using var client = factory.CreateHttpsClient();
        var endpoint = requestName is null ? null : HttpTestRequests.Contracts.Single(item => item.Request == requestName);

        for (var index = 0; index < 3; index++)
        {
            using var request = endpoint is null ? new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/csrf")
                : HttpTestRequests.Create(endpoint);

            if (endpoint is not null && endpoint.Method != "GET")
            {
                await HttpTestRequests.AddProofAsync(client, request, factory.Credential(endpoint.Policy));
            }

            using var response = await client.SendAsync(request);
            Assert.Equal(index < 2 ? endpoint?.SuccessStatus ?? 200 : 429, (int)response.StatusCode);

            if (index == 2)
            {
                Assert.True(response.Headers.Contains("Retry-After"));
                Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            }
        }
    }

    [Theory]
    [InlineData("Api:BaseUrl", "http://api.brouj.com")]
    [InlineData("Api:AllowedOrigins:0", "*")]
    [InlineData("Api:AllowedOrigins:0", "https://brouj.com/path")]
    [InlineData("Api:TrustedProxies:0", "invented-proxy")]
    [InlineData("Api:JsonBodyLimitBytes", "0")]
    [InlineData("Api:MultipartBodyLimitBytes", "0")]
    [InlineData("Api:MaximumJsonDepth", "0")]
    [InlineData("RateLimits:PublicReads:PermitLimit", "0")]
    [InlineData("RateLimits:Authentication:WindowSeconds", "0")]
    [InlineData("RateLimits:ReservationWrites:PermitLimit", "0")]
    [InlineData("RateLimits:AdminWrites:PermitLimit", "0")]
    [InlineData("RateLimits:FileUploads:PermitLimit", "0")]
    public void Invalid_or_missing_required_configuration_fails_before_startup(string key, string value)
    {
        var values = ApiTestConfiguration.Create();
        values[key] = value;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApiServices(configuration));
    }

    [Fact]
    public void Absent_limits_never_fall_back_to_production_guesses()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddApiServices(configuration));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Oversized_JSON_is_413_with_and_without_content_length(bool chunked)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        var json = JsonSerializer.Serialize(new { Email = "a@b.test", Password = new string('x', 20000) });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/customers/login")
        {
            Content = chunked ? new ChunkedJsonContent(json) : new StringContent(json, Encoding.UTF8, "application/json")
        };
        await HttpTestRequests.AddProofAsync(client, request, "");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Theory]
    [InlineData("text/plain", "{}", 415)]
    [InlineData("application/xml", "<login />", 415)]
    [InlineData("application/json", "{ malformed", 400)]
    [InlineData("application/json", "{\"email\":\"a@b.test\",\"password\":\"secret\",\"customerId\":\"11111111-1111-1111-1111-111111111111\"}", 400)]
    public async Task Unsupported_content_malformed_JSON_and_overposting_are_rejected(string type, string body, int status)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/customers/login")
        {
            Content = new StringContent(body, Encoding.UTF8, type)
        };
        await HttpTestRequests.AddProofAsync(client, request, "");

        using var response = await client.SendAsync(request);

        Assert.Equal(status, (int)response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Fact]
    public async Task Configured_JSON_depth_is_enforced_before_dispatch()
    {
        using var factory = new Part8ApiFactory(true, new() { ["Api:MaximumJsonDepth"] = "4" });
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/projects/{Guid.NewGuid()}/time-plan")
        {
            Content = JsonContent.Create(new
            {
                Input = new { Name = "Plan" },
                Stages = new[] { new { Name = "Stage", Items = new[] { new { Name = "Item" } } } }
            })
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Fact]
    public async Task CSRF_proofs_cannot_be_reused_after_the_authenticated_identity_changes()
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/customers/logout");
        await HttpTestRequests.AddProofAsync(client, request, "");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.Credential("CustomerOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Theory]
    [InlineData("customers/login")]
    [InlineData("customers/refresh")]
    [InlineData("customers/logout")]
    [InlineData("users/login")]
    [InlineData("users/refresh")]
    [InlineData("users/logout")]
    public async Task Browser_authentication_requires_CSRF_even_without_an_access_cookie(string route)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/" + route)
        {
            Content = JsonContent.Create(new { Email = "test@example.test", Password = "private-password" })
        };
        request.Headers.Add("Cookie", "__Host-brouj-refresh=private-refresh");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains("private-password", StringComparison.Ordinal)
            || message.Contains("private-refresh", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(5242880, 201)]
    [InlineData(5242881, 413)]
    [InlineData(0, 400)]
    public async Task Image_transport_preserves_the_5_MiB_file_limit(int size, int status)
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/projects/{Guid.NewGuid()}/images")
        {
            Content = HttpTestRequests.ImageContent(size)
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(status, (int)response.StatusCode);
    }

    [Fact]
    public async Task Multiple_files_are_rejected_before_dispatch()
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var content = HttpTestRequests.ImageContent();
        content.Add(new ByteArrayContent(new byte[16]), "File", "second.png");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/projects/{Guid.NewGuid()}/images")
        {
            Content = content
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(factory.Recording.LastRequest);
    }

    [Theory]
    [InlineData("image.png", false)]
    [InlineData("image.exe", true)]
    [InlineData("../image.png", true)]
    public async Task Real_storage_rejects_invalid_signatures_types_and_paths(string name, bool signature)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var id = Guid.NewGuid();
        factory.Database.BaseProjects.Add(new Brouj.Domain.Entities.BaseProject { Id = id });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/projects/{id}/images")
        {
            Content = HttpTestRequests.ImageContent(16, name, signature)
        };
        await HttpTestRequests.AddProofAsync(client, request, factory.Credential("AdminOnly"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.Database.SaveCount);
    }

    [Fact]
    public async Task Production_errors_and_logs_exclude_exception_messages_and_request_secrets()
    {
        const string secret = "private-secret-SQL-password-token";
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        factory.Recording.Exception = new InvalidOperationException(secret);

        using var response = await client.GetAsync("/api/v1/cities?secret=" + secret);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain(secret, await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(secret, StringComparison.Ordinal));
        Assert.Contains(factory.Logs.Messages, message => message.Contains("HTTP route", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Health_exposes_only_liveness_and_production_OpenAPI_is_unavailable()
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var health = await client.GetAsync("/health/live");
        Assert.Equal("{\"status\":\"Healthy\"}", await health.Content.ReadAsStringAsync());
        Assert.Equal("DENY", Assert.Single(health.Headers.GetValues("X-Frame-Options")));
        Assert.True(health.Headers.Contains("Strict-Transport-Security"));
        Assert.Equal("no-store", health.Headers.CacheControl!.ToString());
        using var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.NotFound, openApi.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Forwarded_scheme_and_address_are_accepted_only_from_explicit_proxies(bool trusted)
    {
        using var factory = new Part8ApiFactory(true, new() { ["Api:TrustedProxies:0"] = "192.0.2.10" });
        using var client = factory.CreateHttpsClient();
        var context = await factory.Server.SendAsync(http =>
        {
            http.Request.Scheme = "https";
            http.Request.Host = new Microsoft.AspNetCore.Http.HostString("brouj.test");
            http.Request.Path = "/__tests/transport/connection";
            http.Connection.RemoteIpAddress = IPAddress.Parse(trusted ? "192.0.2.10" : "192.0.2.11");
            http.Request.Headers["X-Forwarded-For"] = "198.51.100.20";
            http.Request.Headers["X-Forwarded-Proto"] = "http";
        });

        Assert.Equal(trusted ? 307 : 200, context.Response.StatusCode);

        if (trusted)
        {
            Assert.Equal("http", context.Request.Scheme);
            Assert.Equal("198.51.100.20", context.Connection.RemoteIpAddress!.ToString());
        }
        else
        {
            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal("192.0.2.11", context.Connection.RemoteIpAddress!.ToString());
        }
    }

    [Fact]
    public async Task Empty_proxy_configuration_does_not_implicitly_trust_loopback()
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        var context = await factory.Server.SendAsync(http =>
        {
            http.Request.Scheme = "https";
            http.Request.Host = new Microsoft.AspNetCore.Http.HostString("brouj.test");
            http.Request.Path = "/health/live";
            http.Connection.RemoteIpAddress = IPAddress.Loopback;
            http.Request.Headers["X-Forwarded-For"] = "198.51.100.20";
            http.Request.Headers["X-Forwarded-Proto"] = "http";
        });

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal(IPAddress.Loopback, context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task HTTPS_redirect_uses_the_configured_port_and_untrusted_hosts_are_rejected()
    {
        using var factory = new Part8ApiFactory();
        using var client = factory.CreateHttpsClient();
        using var redirect = await client.GetAsync("http://brouj.test/health/live");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirect.StatusCode);
        Assert.Equal("https://brouj.test/health/live", redirect.Headers.Location!.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Host = "untrusted.example";
        using var rejected = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.False(rejected.Headers.Contains("Location"));
    }
}
