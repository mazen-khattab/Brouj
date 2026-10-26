using Brouj.Application.Features.Projects.DTOs;
using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetProjectsParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public Guid? CityId { get; init; }

    public Guid? AreaId { get; init; }

    public ProjectType? ProjectType { get; init; }
}
