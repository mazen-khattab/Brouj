using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Brouj.API.IntegrationTests.Support;
using Brouj.Application.Abstractions.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.API.IntegrationTests;

public sealed class Part10AuthenticationSecurityTests
{
    [Theory]
    [InlineData("customers")]
    [InlineData("users")]
    public async Task Wrong_missing_and_deleted_accounts_share_one_response_and_do_not_leak_login_fields(string realm)
    {
        const string knownEmail = "private-login-marker@example.test";
        const string missingEmail = "missing-private-marker@example.test";
        const string password = "Synthetic-private-password-123!";
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var hash = factory.Services.GetRequiredService<IPasswordHasher>().Hash(password);

        if (realm == "customers")
        {
            var account = factory.Database.Customers.Single(customer => customer.Id == factory.CustomerId);
            account.Email = knownEmail;
            account.HashPassword = hash;
        }
        else
        {
            var account = factory.Database.Users.Single(user => user.Id == factory.AdminId);
            account.Email = knownEmail;
            account.HashPassword = hash;
        }

        async Task<string> LoginAsync(string email, string suppliedPassword)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/login")
            {
                Content = JsonContent.Create(new { Email = email, Password = suppliedPassword })
            };
            await HttpTestRequests.AddProofAsync(client, request, "");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));

            return await response.Content.ReadAsStringAsync();
        }

        var wrongPassword = await LoginAsync(knownEmail, "wrong-password-marker");
        var missing = await LoginAsync(missingEmail, password);

        if (realm == "customers")
        {
            factory.Database.Customers.Single(customer => customer.Id == factory.CustomerId).DeletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            factory.Database.Users.Single(user => user.Id == factory.AdminId).DeletedAt = DateTimeOffset.UtcNow;
        }

        var deleted = await LoginAsync(knownEmail, password);

        Assert.Equal(wrongPassword, missing);
        Assert.Equal(wrongPassword, deleted);
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains(knownEmail, StringComparison.Ordinal) ||
            message.Contains(missingEmail, StringComparison.Ordinal) || message.Contains(password, StringComparison.Ordinal) ||
            message.Contains("wrong-password-marker", StringComparison.Ordinal) || message.Contains(hash, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("customers")]
    [InlineData("users")]
    public async Task Actual_login_attempts_are_throttled_and_forwarded_IP_spoofing_cannot_reset_the_bucket(string realm)
    {
        using var factory = new Part8ApiFactory(false, new() { ["RateLimits:Authentication:PermitLimit"] = "3" });
        using var client = factory.CreateHttpsClient();
        using var proof = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/login");

        // One bootstrap GET consumes the first permit. Reuse its anonymous proof for two failed
        // logins, then verify that another password attempt is rejected before handler execution.
        await HttpTestRequests.AddProofAsync(client, proof, "");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/auth/{realm}/login")
            {
                Content = JsonContent.Create(new { Email = "unknown@example.test", Password = "synthetic-wrong-password" })
            };
            request.Headers.Add("Cookie", proof.Headers.GetValues("Cookie"));
            request.Headers.Add("X-CSRF-TOKEN", proof.Headers.GetValues("X-CSRF-TOKEN"));
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{attempt + 1}");

            using var response = await client.SendAsync(request);

            Assert.Equal(attempt < 2 ? HttpStatusCode.Unauthorized : HttpStatusCode.TooManyRequests, response.StatusCode);

            if (attempt == 2)
            {
                Assert.True(response.Headers.Contains("Retry-After"));
            }
        }
    }

    [Theory]
    [InlineData("CustomerOnly", "/api/v1/customers/me", 401)]
    [InlineData("AdminOnly", "/api/v1/users/me", 403)]
    public async Task An_unexpired_access_token_cannot_access_a_deleted_account(string actor, string path, int status)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        var credential = factory.Credential(actor);

        if (actor == "CustomerOnly")
        {
            factory.Database.Customers.Single(customer => customer.Id == factory.CustomerId).DeletedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            factory.Database.Users.Single(user => user.Id == factory.AdminId).DeletedAt = DateTimeOffset.UtcNow;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);

        using var response = await client.GetAsync(path);

        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
    }

    [Theory]
    [InlineData("log-marker\r\nFORGED@example.test")]
    [InlineData("log-marker\u2028FORGED@example.test")]
    public async Task Login_log_forging_payloads_are_never_written_to_logs_or_response_headers(string email)
    {
        using var factory = new Part8ApiFactory(false);
        using var client = factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/users/login")
        {
            Content = JsonContent.Create(new { Email = email, Password = "synthetic-password-marker" })
        };
        await HttpTestRequests.AddProofAsync(client, request, "");

        using var response = await client.SendAsync(request);

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized });
        Assert.DoesNotContain(factory.Logs.Messages, message => message.Contains("log-marker", StringComparison.Ordinal) ||
            message.Contains("FORGED", StringComparison.Ordinal) || message.Contains("synthetic-password-marker", StringComparison.Ordinal));
        Assert.DoesNotContain(response.Headers, header => header.Value.Any(value => value.Contains("FORGED", StringComparison.Ordinal)));
    }
}
