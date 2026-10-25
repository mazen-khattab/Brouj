using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Projects.DTOs;

/// <summary>Explicit Project Input contract; excludes persistence and credential secrets.</summary>
public sealed class ProjectInput
{
    public string Name { get; init; } = string.Empty;

    public Guid AreaId { get; init; }

    public string? Description { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }

    public decimal Size { get; init; }

    public ProjectType ProjectType { get; init; }

    public decimal Price { get; init; }

    public int FloorCount { get; init; }

    public int PlannedUnitCount { get; init; }

    public decimal? NeighborPrice { get; init; }

    public DateTimeOffset? DrawDate { get; init; }
}
