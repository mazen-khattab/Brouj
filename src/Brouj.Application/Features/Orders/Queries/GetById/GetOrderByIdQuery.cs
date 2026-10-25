using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.DTOs;
using MediatR;

namespace Brouj.Application.Features.Orders.Queries.GetById;

public sealed class GetOrderByIdQuery : IRequest<Result<OrderDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Member;

    public Guid Id { get; init; }
}
