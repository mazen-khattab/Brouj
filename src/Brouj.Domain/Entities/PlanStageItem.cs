using Brouj.Domain.Enums;

namespace Brouj.Domain.Entities;

public sealed class PlanStageItem
{
    public Guid Id { get; set; }
    public Guid StageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlanStageItemStatus Status { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? ConfirmedDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public TimePlanStage Stage { get; set; } = null!;
    public ICollection<StageItemImage> StageItemImages { get; set; } = [];
}
