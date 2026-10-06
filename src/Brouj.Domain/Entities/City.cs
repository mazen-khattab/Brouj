namespace Brouj.Domain.Entities;

public sealed class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Area> Areas { get; set; } = [];
}
