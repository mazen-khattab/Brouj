using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.RefreshSession;

public sealed class RefreshCustomerSessionCommand : IRequest<Result<AuthSessionDto>>
{

}
