using System.Security.Claims;
using Brouj.Domain.Enums;

namespace Brouj.Application.Abstractions.Authentication;

public interface IJwtTokenService
{
    string GenerateCustomerAccessToken(Guid customerId);

    string GenerateUserAccessToken(
        Guid userId,
        UserRole role);

    string GenerateRefreshToken();

    ClaimsPrincipal ValidateAccessToken(string token);
}
