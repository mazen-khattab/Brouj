namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query parameters for city lookup operations.</summary>
public sealed class GetCitiesLookupParameters
{
    public string? Search { get; init; }
}
