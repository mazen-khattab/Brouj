using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class BaseProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid AreaId { get; set; }
    public string? Description { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal Size { get; set; }
    public ProjectType ProjectType { get; set; }
    public decimal Price { get; set; }
    public int FloorCount { get; set; }
    public int PlannedUnitCount { get; set; }
    public decimal? GaragePrice { get; set; }
    public bool IsGarage { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Area Area { get; set; } = null!;
    public ICollection<BaseUnit> BaseUnits { get; set; } = [];
    public ICollection<ProjectAmenity> ProjectAmenities { get; set; } = [];
    public ICollection<ProjectImage> ProjectImages { get; set; } = [];
    public InitiativeProject? InitiativeProject { get; set; }
    public TimePlan? TimePlan { get; set; }
}
