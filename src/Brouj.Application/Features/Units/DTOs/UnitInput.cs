
namespace Brouj.Application.Features.Units.DTOs;

/// <summary>Explicit Unit Input contract; excludes persistence and credential secrets.</summary>
public sealed class UnitInput
{
    public string Number { get; init; } = string.Empty;

    public decimal MeterPrice { get; init; }

    public int FloorNumber { get; init; }

    public decimal Size { get; init; }

    public decimal? NeighborMeterPrice { get; init; }
}
