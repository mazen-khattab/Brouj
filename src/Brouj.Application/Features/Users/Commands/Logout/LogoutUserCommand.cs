using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Logout;

public sealed class LogoutUserCommand : IRequest<Result<Guid>>
{

}
