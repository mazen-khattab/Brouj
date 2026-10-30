using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Cities.Queries.GetLookup;

/// <summary>
/// Query to retrieve a lightweight lookup collection of cities.
/// </summary>
public sealed class GetCitiesLookupQuery : IRequest<Result<IEnumerable<LookupDto>>>
{
    public string? Search { get; init; }

    public GetCitiesLookupQuery()
    {
    }

    public GetCitiesLookupQuery(string? search)
    {
        Search = search;
    }
}
