namespace Brouj.Domain.Entities;

public sealed class StageTemplateItem
{
    public Guid Id { get; set; }
    public Guid StageTemplateId { get; set; }
    public Guid ItemTemplateId { get; set; }
    public int SortOrder { get; set; }

    public StageTemplate StageTemplate { get; set; } = null!;
    public ItemTemplate ItemTemplate { get; set; } = null!;
}
