using Brouj.Application.Features.Orders.DTOs;
using Brouj.Domain.Enums;

namespace Brouj.API.Requests;

/// <summary>Explicit HTTP query; route identifiers and access requirements are bound separately.</summary>
public sealed class GetMyOrdersParameters
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public OrderStatus? Status { get; init; }
}
