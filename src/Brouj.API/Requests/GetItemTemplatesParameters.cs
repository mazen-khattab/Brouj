namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetItemTemplatesParameters : PagedSearchParameters
{
    public required bool? IsActive { get; init; }
}
