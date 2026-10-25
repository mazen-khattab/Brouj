using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.RefreshSession;

public sealed class RefreshUserSessionCommand : IRequest<Result<AuthSessionDto>>
{

}
