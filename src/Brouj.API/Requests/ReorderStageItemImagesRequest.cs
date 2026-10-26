using Brouj.Application.Features.TimePlans.DTOs;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class ReorderStageItemImagesRequest
{
    public IReadOnlyList<ReorderItemInput> Items { get; init; } = [];
}
