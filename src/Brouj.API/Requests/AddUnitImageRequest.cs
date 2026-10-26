using Brouj.Application.Common.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class AddUnitImageRequest
{
    public IFormFile File { get; init; } = null!;

    public string? AltText { get; init; }
}
