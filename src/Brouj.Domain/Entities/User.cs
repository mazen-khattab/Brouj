using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string HashPassword { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
