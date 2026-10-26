using Brouj.Application.Features.Units.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetUnitByIdParameters
{
    public Guid ProjectId { get; init; }
}
