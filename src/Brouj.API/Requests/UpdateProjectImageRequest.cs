namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class UpdateProjectImageRequest
{
    public string? AltText { get; init; }
}
