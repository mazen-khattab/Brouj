using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.Logout;

public sealed class LogoutCustomerCommand : IRequest<Result<Guid>>
{

}
