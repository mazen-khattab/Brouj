
namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Auth Session Dto contract; excludes persistence and credential secrets.</summary>
public sealed class AuthSessionDto
{
    public Guid ActorId { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }
}
