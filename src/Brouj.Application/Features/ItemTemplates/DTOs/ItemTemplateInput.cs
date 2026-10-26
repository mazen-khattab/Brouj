
namespace Brouj.Application.Features.ItemTemplates.DTOs;

/// <summary>Explicit Item Template Input contract; excludes persistence and credential secrets.</summary>
public sealed class ItemTemplateInput
{
    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
