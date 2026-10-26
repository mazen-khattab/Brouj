using Brouj.Application.Features.Terms.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetTermsAndConditionsVersionsParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
