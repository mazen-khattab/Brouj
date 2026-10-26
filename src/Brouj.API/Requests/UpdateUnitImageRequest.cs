namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class UpdateUnitImageRequest
{
    public string? AltText { get; init; }
}
