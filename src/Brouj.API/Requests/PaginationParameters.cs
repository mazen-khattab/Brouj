namespace Brouj.API.Requests;

/// <summary>Base HTTP query parameters for paginated operations.</summary>
public class PaginationParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
