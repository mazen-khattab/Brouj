using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Areas.Queries.GetLookup;

/// <summary>
/// Query to retrieve a lightweight lookup collection of areas.
/// </summary>
public sealed class GetAreasLookupQuery : IRequest<Result<IEnumerable<LookupDto>>>
{
    public string? Search { get; init; }

    public GetAreasLookupQuery()
    {
    }

    public GetAreasLookupQuery(string? search)
    {
        Search = search;
    }
}
