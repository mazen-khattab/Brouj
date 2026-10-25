using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Areas.DTOs;

/// <summary>Explicit Area List Dto contract; excludes persistence and credential secrets.</summary>
public sealed class AreaListDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public Guid CityId { get; init; }

    public string CityName { get; init; } = string.Empty;
}
