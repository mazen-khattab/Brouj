using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Orders.Queries.GetAll;

public sealed class GetOrdersQuery : IRequest<Result<PaginationResult<OrderDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public OrderStatus? Status { get; init; }
}
