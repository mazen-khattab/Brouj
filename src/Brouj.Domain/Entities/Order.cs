using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class Order
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public OrderStatus Status { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
