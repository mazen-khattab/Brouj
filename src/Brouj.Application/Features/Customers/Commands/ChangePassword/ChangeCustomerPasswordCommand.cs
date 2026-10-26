using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.ChangePassword;

public sealed class ChangeCustomerPasswordCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;

    public PasswordChangeInput Input { get; init; } = new();
}
