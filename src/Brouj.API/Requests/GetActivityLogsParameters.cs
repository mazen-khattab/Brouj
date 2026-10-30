using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetActivityLogsParameters : PaginationParameters
{
    public Guid? UserId { get; init; }

    public string? EntityType { get; init; }

    public ActivityAction? Action { get; init; }

    public DateTimeOffset? DateFrom { get; init; }

    public DateTimeOffset? DateTo { get; init; }
}
