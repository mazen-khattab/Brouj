namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetCustomersParameters : PagedSearchParameters
{
    public bool? IsDeleted { get; init; }
}
