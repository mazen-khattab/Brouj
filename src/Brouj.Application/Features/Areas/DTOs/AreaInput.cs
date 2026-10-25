using Brouj.Domain.Entities;

namespace Brouj.Application.Features.Areas.DTOs;

/// <summary>Explicit Area Input contract; excludes persistence and credential secrets.</summary>
public sealed class AreaInput
{
    public string Name { get; init; } = string.Empty;

    public Guid CityId { get; init; }
}
