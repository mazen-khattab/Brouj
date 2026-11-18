namespace Brouj.Domain.Entities;

public sealed class InitiativeReservation
{
    public Guid ReservationId { get; set; }
    public string IdentityNumber { get; set; } = string.Empty;
    public string AgeGroup { get; set; } = string.Empty;
    public string PlaceOfResidence { get; set; } = string.Empty;
    public int FinancialBracket { get; set; }
    public int RepaymentPeriod { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
