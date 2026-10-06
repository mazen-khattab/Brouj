namespace Brouj.Domain.Entities;

public sealed class BaseUnit
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal MeterPrice { get; set; }
    public int FloorNumber { get; set; }
    public decimal Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseProject Project { get; set; } = null!;
    public InitiativeUnit? InitiativeUnit { get; set; }
    public ICollection<UnitAmenity> UnitAmenities { get; set; } = [];
    public ICollection<UnitImage> UnitImages { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
}
