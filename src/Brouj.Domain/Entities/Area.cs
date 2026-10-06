namespace Brouj.Domain.Entities;

public sealed class Area
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid CityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public City City { get; set; } = null!;
    public ICollection<AreaAmenity> AreaAmenities { get; set; } = [];
    public ICollection<BaseProject> BaseProjects { get; set; } = [];
}
