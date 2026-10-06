namespace Brouj.Domain.Entities;

public sealed class StageItemImage
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public PlanStageItem Item { get; set; } = null!;
}
