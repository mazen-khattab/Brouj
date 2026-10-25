using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.Register;

public sealed class RegisterCustomerCommand : IRequest<Result<AuthSessionDto>>
{
    public RegisterCustomerInput Input { get; init; } = new();
}
