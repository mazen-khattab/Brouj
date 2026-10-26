using System.Security.Claims;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Enums;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureJwt : IJwtTokenService
{
    public string GenerateCustomerAccessToken(Guid customerId) => customerId.ToString();

    public string GenerateUserAccessToken(Guid userId, UserRole role) => userId.ToString();

    public string GenerateRefreshToken() => "synthetic-refresh";

    public ClaimsPrincipal ValidateAccessToken(string token) =>
        new(new ClaimsIdentity([new Claim("sub", token)], "test"));
}
