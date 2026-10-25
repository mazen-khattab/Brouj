
namespace Brouj.Application.Common.DTOs;

/// <summary>Explicit Amenity Dto contract; excludes persistence and credential secrets.</summary>
public sealed class AmenityDto
{
    public string Name { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}
