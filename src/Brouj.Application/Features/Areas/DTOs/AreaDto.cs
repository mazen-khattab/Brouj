using Brouj.Application.Common.DTOs;
using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Areas.DTOs;

/// <summary>Explicit Area Dto contract; excludes persistence and credential secrets.</summary>
public sealed class AreaDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Guid CityId { get; init; }

    public string CityName { get; init; } = string.Empty;

    public IReadOnlyList<AmenityDto> Amenities { get; init; } = [];
}
