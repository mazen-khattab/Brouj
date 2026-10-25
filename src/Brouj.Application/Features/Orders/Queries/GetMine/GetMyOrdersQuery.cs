using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Orders.Queries.GetMine;

public sealed class GetMyOrdersQuery : IRequest<Result<PaginationResult<OrderDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public OrderStatus? Status { get; init; }
}
