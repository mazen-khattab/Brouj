namespace Brouj.Domain.Entities;

public sealed class UnitImage
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseUnit Unit { get; set; } = null!;
}
