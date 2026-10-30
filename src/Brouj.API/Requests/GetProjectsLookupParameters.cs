using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query parameters for project lookup operations.</summary>
public sealed class GetProjectsLookupParameters
{
    public string? Search { get; init; }

    public ProjectType? Type { get; init; }
}
