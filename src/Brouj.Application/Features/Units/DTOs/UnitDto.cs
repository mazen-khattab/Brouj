using Brouj.Application.Common.DTOs;

namespace Brouj.Application.Features.Units.DTOs;

/// <summary>Explicit Unit Dto contract; excludes persistence and credential secrets.</summary>
public sealed class UnitDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string Number { get; init; } = string.Empty;

    public decimal MeterPrice { get; init; }

    public int FloorNumber { get; init; }

    public decimal Size { get; init; }

    public decimal? NeighborMeterPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public decimal? Percentage { get; init; }

    public bool IsAvailable { get; init; }

    public bool IsGarage { get; init; } = true;

    public decimal GaragePrice { get; init; } = 0m;

    public IReadOnlyList<AmenityDto> Amenities { get; set; } = [];

    public IReadOnlyList<ImageMetadataDto> Images { get; set; } = [];
}
