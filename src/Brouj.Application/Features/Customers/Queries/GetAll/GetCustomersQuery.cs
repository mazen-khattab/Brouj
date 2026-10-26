using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;

namespace Brouj.Application.Features.Customers.Queries.GetAll;

public sealed class GetCustomersQuery : IRequest<Result<PaginationResult<CustomerDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public bool? IsDeleted { get; init; }
}
