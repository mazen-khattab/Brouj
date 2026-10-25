
namespace Brouj.Application.Features.TimePlans.DTOs;

/// <summary>Explicit Time Plan Input contract; excludes persistence and credential secrets.</summary>
public sealed class TimePlanInput
{
    public string Name { get; init; } = string.Empty;

    public DateTimeOffset? DeliveryDate { get; init; }
}
