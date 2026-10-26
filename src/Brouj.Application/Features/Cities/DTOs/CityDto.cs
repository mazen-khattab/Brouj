using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Cities.DTOs;

/// <summary>Explicit City Dto contract; excludes persistence and credential secrets.</summary>
public sealed class CityDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
