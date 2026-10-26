using Brouj.Application.Common.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class ReplaceUnitAmenitiesRequest
{
    public IReadOnlyList<AmenityDto> Amenities { get; init; } = [];
}
