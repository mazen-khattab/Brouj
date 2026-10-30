namespace Brouj.API.Requests;

/// <summary>Base HTTP query parameters for paginated operations with search filtering.</summary>
public class PagedSearchParameters : PaginationParameters
{
    public string? Search { get; init; }
}
