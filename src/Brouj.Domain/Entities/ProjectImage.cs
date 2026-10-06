namespace Brouj.Domain.Entities;

public sealed class ProjectImage
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public BaseProject Project { get; set; } = null!;
}
