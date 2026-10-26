using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;

namespace Brouj.Application.Features.Customers.Queries.GetCurrent;

public sealed class GetCurrentCustomerQuery : IRequest<Result<CurrentCustomerDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

}
