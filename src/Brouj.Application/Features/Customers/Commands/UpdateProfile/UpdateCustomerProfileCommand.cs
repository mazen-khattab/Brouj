using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.UpdateProfile;

public sealed class UpdateCustomerProfileCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

    public CustomerProfileInput Input { get; init; } = new();
}
