using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Login;

public sealed class LoginUserCommand : IRequest<Result<AuthSessionDto>>
{
    public LoginInput Input { get; init; } = new();
}
