using Brouj.Application.Features.Areas.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetAreasParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}
