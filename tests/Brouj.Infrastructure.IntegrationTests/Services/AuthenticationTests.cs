using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Brouj.Application.Common.Settings;
using Brouj.Domain.Enums;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class AuthenticationTests
{
    internal static readonly DateTimeOffset Now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
    internal static JwtOptions Settings(string? key = null) => new()
    {
        Issuer = "brouj-tests", Audience = "brouj-test-clients",
        SigningKey = key ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    };
    internal static JwtTokenService Service(JwtOptions? settings = null, TimeProvider? clock = null) =>
        new(Options.Create(settings ?? Settings()), clock ?? new FixedClock(Now));

    [Fact]
    public void Argon2id_hash_has_approved_cost_unique_salts_and_supports_verification()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("synthetic-test-password");
        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", hash);
        Assert.NotEqual(hash, hasher.Hash("synthetic-test-password"));
        Assert.True(hasher.Verify("synthetic-test-password", hash));
        Assert.False(hasher.Verify("wrong-password", hash));
        Assert.False(hasher.NeedsRehash(hash));
        Assert.True(hash.Length <= 256);
    }

    [Fact]
    public void Older_valid_parameters_verify_and_require_rehash()
    {
        var old = Argon2.Hash("synthetic-password", timeCost: 1, memoryCost: 8192, parallelism: 1);
        var hasher = new PasswordHasher();
        Assert.True(hasher.Verify("synthetic-password", old));
        Assert.True(hasher.NeedsRehash(old));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("$argon2id$v=19$m=2147483647,t=2,p=1$YWJjZGVmZ2hpamtsbW5vcA$YWJjZGVmZ2hpamtsbW5vcA")]
    public void Malformed_or_unbounded_hashes_are_rejected(string hash)
    {
        var hasher = new PasswordHasher();
        Assert.False(hasher.Verify("synthetic-password", hash));
        Assert.True(hasher.NeedsRehash(hash));
    }

    [Theory]
    [InlineData(false, UserRole.Admin)]
    [InlineData(true, UserRole.Admin)]
    [InlineData(true, UserRole.SuperAdmin)]
    public void Tokens_have_minimal_actor_claims_and_current_user_exposes_only_that_actor(bool user, UserRole role)
    {
        var jwt = Service();
        var id = Guid.NewGuid();
        var token = user ? jwt.GenerateUserAccessToken(id, role) : jwt.GenerateCustomerAccessToken(id);
        var principal = jwt.ValidateAccessToken(token);
        Assert.Equal(id.ToString("D"), principal.FindFirst("sub")!.Value);
        Assert.Equal(user ? "user" : "customer", principal.FindFirst("actor_type")!.Value);
        Assert.Equal(user ? 9 : 8, principal.Claims.Count());
        var context = new DefaultHttpContext { User = principal };
        var actor = new CurrentUser(new HttpContextAccessor { HttpContext = context });
        Assert.True(actor.IsAuthenticated);
        Assert.Equal(user ? id : null, actor.UserId);
        Assert.Equal(user ? null : id, actor.CustomerId);
        Assert.Equal(user ? role : null, actor.Role);
        Assert.Equal(900, long.Parse(principal.FindFirst("exp")!.Value) - long.Parse(principal.FindFirst("iat")!.Value));
    }

    [Fact]
    public void Tampered_wrong_key_issuer_audience_and_expired_tokens_are_rejected()
    {
        var settings = Settings();
        var jwt = Service(settings);
        var token = jwt.GenerateCustomerAccessToken(Guid.NewGuid());
        Assert.ThrowsAny<SecurityTokenException>(() => Service().ValidateAccessToken(token));
        Assert.ThrowsAny<SecurityTokenException>(() => Service(new JwtOptions
            { Issuer = "wrong", Audience = settings.Audience, SigningKey = settings.SigningKey }).ValidateAccessToken(token));
        Assert.ThrowsAny<SecurityTokenException>(() => Service(new JwtOptions
            { Issuer = settings.Issuer, Audience = "wrong", SigningKey = settings.SigningKey }).ValidateAccessToken(token));
        var parts = token.Split('.');
        var payload = Base64UrlEncoder.Decode(parts[1]).Replace("customer", "user", StringComparison.Ordinal);
        Assert.ThrowsAny<SecurityTokenException>(() => jwt.ValidateAccessToken(parts[0] + "." + Base64UrlEncoder.Encode(payload) + "." + parts[2]));
        Assert.ThrowsAny<SecurityTokenException>(() => Service(settings, new FixedClock(Now.AddMinutes(15).AddSeconds(31))).ValidateAccessToken(token));
        Assert.True(Service(settings, new FixedClock(Now.AddMinutes(15).AddSeconds(29))).ValidateAccessToken(token).Identity!.IsAuthenticated);
    }

    [Theory]
    [InlineData("missing-sub")]
    [InlineData("empty-sub")]
    [InlineData("duplicate-sub")]
    [InlineData("unknown-actor")]
    [InlineData("duplicate-actor")]
    [InlineData("customer-with-role")]
    [InlineData("user-without-role")]
    [InlineData("unknown-role")]
    [InlineData("duplicate-role")]
    [InlineData("missing-jti")]
    [InlineData("missing-iat")]
    [InlineData("too-long")]
    [InlineData("future")]
    [InlineData("wrong-algorithm")]
    public void Even_correctly_signed_tokens_must_satisfy_actor_and_metadata_contract(string failure)
    {
        var settings = failure == "wrong-algorithm" ? Settings(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))) : Settings();
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString("D")), new("actor_type", "customer"),
            new("jti", Guid.NewGuid().ToString("N")), new("iat", Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        if (failure == "missing-sub") claims.RemoveAll(c => c.Type == "sub");
        if (failure == "empty-sub") { claims.RemoveAll(c => c.Type == "sub"); claims.Add(new("sub", Guid.Empty.ToString("D"))); }
        if (failure == "duplicate-sub") claims.Add(new("sub", Guid.NewGuid().ToString("D")));
        if (failure == "unknown-actor") { claims.RemoveAll(c => c.Type == "actor_type"); claims.Add(new("actor_type", "other")); }
        if (failure == "duplicate-actor") claims.Add(new("actor_type", "user"));
        if (failure is "customer-with-role" or "unknown-role" or "duplicate-role") claims.Add(new("role", failure == "unknown-role" ? "other" : "admin"));
        if (failure is "user-without-role" or "unknown-role" or "duplicate-role")
        { claims.RemoveAll(c => c.Type == "actor_type"); claims.Add(new("actor_type", "user")); }
        if (failure == "duplicate-role") claims.Add(new("role", "super admin"));
        if (failure == "missing-jti") claims.RemoveAll(c => c.Type == "jti");
        if (failure == "missing-iat") claims.RemoveAll(c => c.Type == "iat");
        var begins = failure == "future" ? Now.AddMinutes(1) : Now;
        var expires = failure == "too-long" ? Now.AddHours(1) : begins.AddMinutes(15);
        // This helper signs adversarial claims using the real key, proving authorization validation
        // is enforced separately from the signature check.
        var keyBytes = Convert.FromBase64String(settings.SigningKey);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, begins.UtcDateTime,
            expires.UtcDateTime, new SigningCredentials(new SymmetricSecurityKey(keyBytes),
                failure == "wrong-algorithm" ? SecurityAlgorithms.HmacSha512 : SecurityAlgorithms.HmacSha256));
        Assert.ThrowsAny<SecurityTokenException>(() => Service(settings).ValidateAccessToken(new JwtSecurityTokenHandler().WriteToken(token)));
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("YWJj")]
    public void Invalid_signing_secrets_fail_closed(string key) =>
        Assert.Throws<InvalidOperationException>(() => Service(Settings(key)));

    [Fact]
    public void Refresh_credentials_are_unique_canonical_and_only_the_digest_is_storable()
    {
        var jwt = Service();
        var raw = jwt.GenerateRefreshToken();
        Assert.NotEqual(raw, jwt.GenerateRefreshToken());
        Assert.Equal(43, raw.Length);
        Assert.True(RefreshTokenDigest.TryHash(raw, out var digest));
        Assert.Equal(64, digest.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(raw))), digest);
        Assert.False(RefreshTokenDigest.TryHash(raw + "=", out _));
        Assert.False(RefreshTokenDigest.TryHash("untrusted", out _));
    }

    [Fact]
    public void Current_user_does_not_accept_untrusted_or_conflicting_actor_claims()
    {
        var identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("actor_type", "user"), new Claim("role", "owner")], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var actor = new CurrentUser(new HttpContextAccessor { HttpContext = context });
        Assert.False(actor.IsAuthenticated);
        Assert.Null(actor.UserId);
        Assert.Null(actor.Role);
        identity = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("actor_type", "customer")]);
        context.User = new ClaimsPrincipal(identity);
        Assert.False(actor.IsAuthenticated);
    }
}
