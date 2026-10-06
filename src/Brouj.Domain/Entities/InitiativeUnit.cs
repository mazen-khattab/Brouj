namespace Brouj.Domain.Entities;

public sealed class InitiativeUnit
{
    public Guid UnitId { get; set; }
    public decimal NeighborMeterPrice { get; set; }
    public decimal Percentage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseUnit Unit { get; set; } = null!;
}
