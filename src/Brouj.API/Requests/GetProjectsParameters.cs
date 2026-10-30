using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetProjectsParameters : PagedSearchParameters
{
    public Guid? CityId { get; init; }

    public Guid? AreaId { get; init; }

    public ProjectType? ProjectType { get; init; }
}
