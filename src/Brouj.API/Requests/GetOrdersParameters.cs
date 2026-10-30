using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetOrdersParameters : PaginationParameters
{
    public OrderStatus? Status { get; init; }
}
