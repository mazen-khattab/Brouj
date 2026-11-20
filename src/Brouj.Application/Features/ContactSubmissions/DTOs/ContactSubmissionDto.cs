namespace Brouj.Application.Features.ContactSubmissions.DTOs;

public sealed class ContactSubmissionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Inquiry { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
