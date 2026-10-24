using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Brouj.Infrastructure.Authentication;

/// <summary>Exposes only a valid authenticated actor from the host's trusted authentication principal.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public bool IsAuthenticated => TryActor(out _, out _, out _);
    public Guid? UserId => TryActor(out var id, out var actor, out _) && actor == "user" ? id : null;
    public Guid? CustomerId => TryActor(out var id, out var actor, out _) && actor == "customer" ? id : null;
    public UserRole? Role => TryActor(out _, out _, out var role) ? role : null;

    private bool TryActor(out Guid id, out string actor, out UserRole? role)
    {
        id = Guid.Empty; 
        actor = ""; 
        role = null;

        var principal = accessor.HttpContext?.User;
        return principal?.Identity?.IsAuthenticated == true && TokenClaims.TryActor(principal, out id, out actor, out role);
    }
}
