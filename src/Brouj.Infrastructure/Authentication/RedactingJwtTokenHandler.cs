using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Removes credential-bearing validation diagnostics before the HTTP handler can log them.</summary>
internal sealed class RedactingJwtTokenHandler : JsonWebTokenHandler
{
    public RedactingJwtTokenHandler()
    {
        MapInboundClaims = false;
        MaximumTokenSizeInBytes = JwtTokenService.MaximumTokenBytes;
    }

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        // Signature, issuer, audience and lifetime checks remain in Microsoft's trusted handler.
        var result = await base.ValidateTokenAsync(token, validationParameters);

        if (result.IsValid)
        {
            return result;
        }

        // JwtBearer logs failures BEFORE OnAuthenticationFailed. Replace the entire failed result
        // here so neither the original exception nor its token/claim properties reach those logs.
        return new TokenValidationResult
        {
            IsValid = false,
            Exception = new SecurityTokenException("Invalid access token.")
        };
    }
}
