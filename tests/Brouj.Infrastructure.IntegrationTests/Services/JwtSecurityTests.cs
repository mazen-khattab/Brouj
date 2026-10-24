using System.IdentityModel.Tokens.Jwt;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Settings;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class JwtSecurityTests
{
    private const string PrivateValue = "synthetic-private-claim\r\nforged-log-entry";

    [Theory]
    [InlineData("tampered", false)]
    [InlineData("tampered", true)]
    [InlineData("expired", false)]
    [InlineData("expired", true)]
    [InlineData("issuer", false)]
    [InlineData("issuer", true)]
    [InlineData("audience", false)]
    [InlineData("audience", true)]
    [InlineData("audience-slash", false)]
    [InlineData("audience-slash", true)]
    [InlineData("type", false)]
    [InlineData("type", true)]
    [InlineData("oversized", false)]
    [InlineData("oversized", true)]
    [InlineData("missing-expiry", false)]
    [InlineData("missing-expiry", true)]
    [InlineData("missing-not-before", false)]
    [InlineData("missing-not-before", true)]
    [InlineData("role", false)]
    [InlineData("role", true)]
    public async Task Host_rejects_invalid_credentials_without_retaining_or_logging_private_details(
        string failure,
        bool cookie)
    {
        using var provider = CookieTests.Provider();
        using var logs = new SecurityLogProvider();
        provider.GetRequiredService<ILoggerFactory>().AddProvider(logs);
        using var scope = provider.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var token = AdversarialToken(jwt, settings, failure);
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Scheme = "https";

        if (cookie)
        {
            context.Request.Headers.Cookie = "__Host-brouj-access=" + token;
        }
        else
        {
            context.Request.Headers.Authorization = "Bearer " + token;
        }

        var result = await context.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Equal("Invalid access token.", result.Failure.Message);
        Assert.Null(result.Failure.InnerException);
        Assert.Null(result.Principal);
        Assert.DoesNotContain(PrivateValue, result.Failure.ToString());
        Assert.NotEmpty(logs.Entries);
        Assert.All(logs.Entries, entry =>
        {
            Assert.DoesNotContain(token, entry, StringComparison.Ordinal);
            Assert.DoesNotContain("synthetic-private-claim", entry, StringComparison.Ordinal);
            Assert.DoesNotContain("forged-log-entry", entry, StringComparison.Ordinal);
            Assert.DoesNotContain(settings.SigningKey, entry, StringComparison.Ordinal);
        });

        // The direct service and the HTTP handler must enforce the same acceptance boundary.
        var error = Assert.Throws<SecurityTokenException>(() => jwt.ValidateAccessToken(token));
        Assert.Equal("Invalid access token.", error.Message);
        Assert.Null(error.InnerException);
    }

    [Fact]
    public async Task Explicit_invalid_authorization_header_cannot_fall_back_to_a_valid_cookie()
    {
        using var provider = CookieTests.Provider();
        using var scope = provider.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers.Cookie = "__Host-brouj-access=" + jwt.GenerateCustomerAccessToken(Guid.NewGuid());
        context.Request.Headers.Authorization = "Bearer invalid-credential";

        var result = await context.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Principal);
    }

    private static string AdversarialToken(
        IJwtTokenService jwt,
        JwtOptions settings,
        string failure)
    {
        var handler = new JwtSecurityTokenHandler();
        var valid = jwt.GenerateCustomerAccessToken(Guid.NewGuid());
        var parsed = handler.ReadJwtToken(valid);
        parsed.Payload["private_test_value"] = PrivateValue;

        switch (failure)
        {
            case "expired":
                var expiredAt = DateTimeOffset.UtcNow.AddMinutes(-2).ToUnixTimeSeconds();
                parsed.Payload["iat"] = expiredAt - 900;
                parsed.Payload["nbf"] = expiredAt - 900;
                parsed.Payload["exp"] = expiredAt;
                break;
            case "issuer":
                parsed.Payload["iss"] = PrivateValue;
                break;
            case "audience":
                parsed.Payload["aud"] = PrivateValue;
                break;
            case "audience-slash":
                parsed.Payload["aud"] = settings.Audience + "/";
                break;
            case "oversized":
                parsed.Payload["padding"] = new string('x', 8192);
                break;
            case "missing-expiry":
                parsed.Payload.Remove("exp");
                break;
            case "missing-not-before":
                parsed.Payload.Remove("nbf");
                break;
            case "role":
                parsed.Payload["role"] = "super admin";
                break;
        }

        var key = new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKey));
        var header = new JwtHeader(new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        if (failure == "type")
        {
            header["typ"] = PrivateValue;
        }

        var token = handler.WriteToken(new JwtSecurityToken(header, parsed.Payload));

        if (failure == "tampered")
        {
            // Change signed payload bytes while retaining the original signature.
            var parts = token.Split('.');
            parts[1] = Base64UrlEncoder.Encode(Base64UrlEncoder.Decode(parts[1]) + " ");
            token = string.Join('.', parts);
        }

        return token;
    }
}
