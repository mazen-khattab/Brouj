namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class DeleteUnitParameters
{
    public Guid ProjectId { get; init; }
}
