namespace Brouj.Domain.Entities;

public sealed class InitiativeProject
{
    public Guid ProjectId { get; set; }
    public decimal NeighborPrice { get; set; }
    public DateTimeOffset DrawDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseProject Project { get; set; } = null!;
}
