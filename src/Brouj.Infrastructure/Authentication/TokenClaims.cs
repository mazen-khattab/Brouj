using System.Globalization;
using System.Security.Claims;
using Brouj.Domain.Enums;

namespace Brouj.Infrastructure.Authentication;

/// <summary>The single actor-claim contract shared by issuance, validation, authorization, and auditing.</summary>
internal static class TokenClaims
{
    internal const string ActorType = "actor_type";
    internal const string Role = "role";
    internal static string RoleName(UserRole role) => role switch
    {
        UserRole.Admin => "admin",
        UserRole.SuperAdmin => "super admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    internal static bool TryActor(ClaimsPrincipal principal, out Guid id, out string actor, out UserRole? role)
    {
        id = Guid.Empty;
        role = null;
        actor = Single(principal, ActorType) ?? "";

        if (!Guid.TryParseExact(Single(principal, "sub"), "D", out id) || id == Guid.Empty)
        {
            return false;
        }

        var roles = principal.FindAll(Role).ToArray();

        if (actor == "customer")
        {
            return roles.Length == 0;
        }

        if (actor != "user" || roles.Length != 1)
        {
            return false;
        }

        role = roles[0].Value switch
        {
            "admin" => UserRole.Admin,
            "super admin" => UserRole.SuperAdmin,
            _ => null
        };

        return role.HasValue;
    }

    internal static bool ValidMetadata(ClaimsPrincipal principal, DateTimeOffset now)
    {
        if (!Guid.TryParseExact(Single(principal, "jti"), "N", out _))
        {
            return false;
        }

        if (!Number("iat", out var issued) || !Number("nbf", out var begins) || !Number("exp", out var expires))
        {
            return false;
        }

        return issued == begins && issued <= now.ToUnixTimeSeconds() + 30 && expires > issued && expires - issued <= 900;

        bool Number(string claim, out long value)
        {
            return long.TryParse(Single(principal, claim), NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }

    private static string? Single(ClaimsPrincipal principal, string name)
    {
        var values = principal.FindAll(name).Take(2).ToArray();
        return values.Length == 1 ? values[0].Value : null;
    }
}
