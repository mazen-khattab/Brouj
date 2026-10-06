using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid UserId { get; set; }
    public ActivityAction Action { get; set; }
    public string? NewData { get; set; }
    public string? OldData { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
