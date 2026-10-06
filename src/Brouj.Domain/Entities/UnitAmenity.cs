namespace Brouj.Domain.Entities;

public sealed class UnitAmenity
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseUnit Unit { get; set; } = null!;
}
