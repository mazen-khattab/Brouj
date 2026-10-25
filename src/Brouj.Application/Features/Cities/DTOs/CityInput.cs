using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Cities.DTOs;

/// <summary>Explicit City Input contract; excludes persistence and credential secrets.</summary>
public sealed class CityInput
{
    public string Name { get; init; } = string.Empty;
}
