using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Projects.Queries.GetLookup;

/// <summary>
/// Query to retrieve a lightweight lookup collection of projects filtered by search term and project type.
/// </summary>
public sealed class GetProjectsLookupQuery : IRequest<Result<IEnumerable<LookupDto>>>
{
    public string? Search { get; init; }

    public ProjectType? Type { get; init; }

    public GetProjectsLookupQuery()
    {
    }

    public GetProjectsLookupQuery(string? search, ProjectType? type = null)
    {
        Search = search;
        Type = type;
    }
}
