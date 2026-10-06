namespace Brouj.Domain.Entities;

public sealed class TimePlanStage
{
    public Guid Id { get; set; }
    public Guid TimePlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset DeliveryDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public TimePlan TimePlan { get; set; } = null!;
    public ICollection<PlanStageItem> PlanStageItems { get; set; } = [];
}
