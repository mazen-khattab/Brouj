namespace Brouj.Domain.Entities;

public sealed class CustomerRefreshToken
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset ExpDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
