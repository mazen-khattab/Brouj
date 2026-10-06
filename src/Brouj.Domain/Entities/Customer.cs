namespace Brouj.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; set; }
    public string FName { get; set; } = string.Empty;
    public string LName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HashPassword { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<CustomerRefreshToken> CustomerRefreshTokens { get; set; } = [];
}
