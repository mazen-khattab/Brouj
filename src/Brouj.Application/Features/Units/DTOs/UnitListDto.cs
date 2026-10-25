
namespace Brouj.Application.Features.Units.DTOs;

/// <summary>Explicit Unit List Dto contract; excludes persistence and credential secrets.</summary>
public sealed class UnitListDto
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
}
