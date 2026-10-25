
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Reorder Item Input contract; excludes persistence and credential secrets.</summary>
public sealed class ReorderItemInput
{
    public Guid Id { get; init; }

    public int SortOrder { get; init; }
}
