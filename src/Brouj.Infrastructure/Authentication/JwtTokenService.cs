using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Settings;
using Brouj.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Issues minimal HS256 access tokens and validates their cryptographic and actor contracts.</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    internal const int MaximumTokenBytes = 8192;

    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;
    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _handler = new()
    {
        MapInboundClaims = false,
        MaximumTokenSizeInBytes = MaximumTokenBytes
    };

    /// <summary>Fails closed when a signing secret or the approved token lifetimes are missing.</summary>
    public JwtTokenService(
        IOptions<JwtOptions> options,
        TimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
        _key = SigningKey(_options);
    }

    /// <summary>Issues a 15-minute customer JWT containing identity, actor type, and standard metadata.</summary>
    public string GenerateCustomerAccessToken(Guid customerId)
    {
        return Generate(customerId, "customer", null);
    }

    /// <summary>Issues a 15-minute staff JWT with the applicable approved role.</summary>
    public string GenerateUserAccessToken(
        Guid userId,
        UserRole role)
    {
        return Generate(userId, "user", TokenClaims.RoleName(role));
    }

    /// <summary>Generates 32 cryptographically random bytes encoded as an opaque Base64url credential.</summary>
    public string GenerateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>Validates HS256, issuer, audience, expiry/skew, required metadata, and the actor/role contract.</summary>
    public ClaimsPrincipal ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaximumTokenBytes)
        {
            throw new SecurityTokenException("Invalid access token.");
        }

        try
        {
            var principal = _handler.ValidateToken(token, ValidationParameters(_options, _clock), out _);
            ValidateClaims(principal, _clock);
            return principal;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            // Normalize malformed payloads as authentication failures. Library diagnostic exceptions
            // can retain the encoded credential, so do not attach them as inner exceptions.
            throw new SecurityTokenException("Invalid access token.");
        }
    }

    internal static void ValidateClaims(
        ClaimsPrincipal principal,
        TimeProvider clock)
    {
        if (!TokenClaims.TryActor(principal, out _, out _, out _) || !TokenClaims.ValidMetadata(principal, clock.GetUtcNow()))
        {
            throw new SecurityTokenException("Invalid access token claims.");
        }
    }

    internal static TokenValidationParameters ValidationParameters(
        JwtOptions options,
        TimeProvider clock) => new()
    {
        ValidateIssuerSigningKey = true, 
        IssuerSigningKey = SigningKey(options), 
        RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256], 
        ValidTypes = ["JWT"],
        ValidateIssuer = true, 
        ValidIssuer = options.Issuer,
        ValidateAudience = true, 
        ValidAudience = options.Audience,
        // The configured audience is exact; a trailing slash must not select a different audience.
        IgnoreTrailingSlashWhenValidatingAudience = false,
        RequireExpirationTime = true, 
        ValidateLifetime = true, 
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub", 
        RoleClaimType = TokenClaims.Role,
        // Failed validation results must not retain credentials or log their token identifiers.
        IncludeTokenOnFailedValidation = false,
        LogTokenId = false,
        SaveSigninToken = false,
        // Share the same clock with issuance and tests, preserving the approved 30-second skew.
        LifetimeValidator = (notBefore, expires, _, _) => expires.HasValue &&
            (!notBefore.HasValue || notBefore.Value <= clock.GetUtcNow().UtcDateTime.AddSeconds(30)) &&
            expires.Value >= clock.GetUtcNow().UtcDateTime.AddSeconds(-30) &&
            (!notBefore.HasValue || notBefore.Value < expires.Value)
    };

    internal static SymmetricSecurityKey SigningKey(JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience) ||
            options.AccessTokenLifetimeMinutes != 15 || options.RefreshTokenLifetimeDays != 7)
        {
            throw new InvalidOperationException("JWT requires issuer, audience, and lifetimes of 15 minutes / 7 days.");
        }
            
        byte[] bytes;

        try
        {
            bytes = Convert.FromBase64String(options.SigningKey);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentNullException)
        {
            throw new InvalidOperationException("JWT signing key must be Base64-encoded random bytes.");
        }

        if (bytes.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must contain at least 32 random bytes.");
        }

        return new SymmetricSecurityKey(bytes);
    }

    private string Generate(
        Guid id,
        string actorType,
        string? role)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An actor ID is required.", nameof(id));
        }

        var now = _clock.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new("sub", id.ToString("D")),
            new(TokenClaims.ActorType, actorType),
            new("jti", Guid.NewGuid().ToString("N"))
        };

        if (role is not null)
        {
            claims.Add(new Claim(TokenClaims.Role, role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(15),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateEncodedJwt(descriptor);
    }
}
