using Brouj.Domain.Enums;

namespace Brouj.Application.Features.ActivityLogs.DTOs;

/// <summary>Explicit Activity Log Dto contract; excludes persistence and credential secrets.</summary>
public sealed class ActivityLogDto
{
    public Guid Id { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public Guid EntityId { get; init; }

    public Guid UserId { get; init; }

    public ActivityAction Action { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
