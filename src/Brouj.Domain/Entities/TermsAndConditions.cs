namespace Brouj.Domain.Entities;

public sealed class TermsAndConditions
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
