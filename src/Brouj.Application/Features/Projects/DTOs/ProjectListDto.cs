using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Projects.DTOs;

/// <summary>Explicit Project List Dto contract; excludes persistence and credential secrets.</summary>
public sealed class ProjectListDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Guid AreaId { get; init; }

    public string AreaName { get; init; } = string.Empty;

    public Guid CityId { get; init; }

    public ProjectType ProjectType { get; init; }

    public decimal Price { get; init; }

    public decimal Size { get; init; }
}
