namespace Brouj.Application.Features.ContactSubmissions.DTOs;

public sealed class ContactSubmissionInput
{
    public string Name
    {
        get;
        init => field = value?.Trim() ?? string.Empty;
    } = string.Empty;

    public string Phone
    {
        get;
        init => field = value?.Trim() ?? string.Empty;
    } = string.Empty;

    public string Inquiry
    {
        get;
        init => field = value?.Trim() ?? string.Empty;
    } = string.Empty;
}
