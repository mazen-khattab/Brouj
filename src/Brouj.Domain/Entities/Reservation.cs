using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class Reservation
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string PlaceOfResidence { get; set; } = string.Empty;
    public int FinancialBracket { get; set; }
    public int RepaymentPeriod { get; set; }
    public DateTimeOffset ReservationDate { get; set; }
    public string Number { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseUnit Unit { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Order? Order { get; set; }
}
