using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;

namespace Brouj.Application.Features.Customers.Queries.GetById;

public sealed class GetCustomerByIdQuery : IRequest<Result<CustomerDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
