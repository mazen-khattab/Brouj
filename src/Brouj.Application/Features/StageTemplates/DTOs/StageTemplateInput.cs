
namespace Brouj.Application.Features.StageTemplates.DTOs;

/// <summary>Explicit Stage Template Input contract; excludes persistence and credential secrets.</summary>
public sealed class StageTemplateInput
{
    public string Name { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;
}
