
namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Image Metadata Dto contract; excludes persistence and credential secrets.</summary>
public sealed class ImageMetadataDto
{
    public Guid Id { get; init; }

    public string ImagePath { get; init; } = string.Empty;

    public string? AltText { get; init; }
}
