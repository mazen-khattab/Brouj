namespace Brouj.Domain.Entities;

public sealed class ContactSubmission
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Inquiry { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
