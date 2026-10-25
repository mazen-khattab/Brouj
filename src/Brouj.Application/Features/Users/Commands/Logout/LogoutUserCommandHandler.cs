using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Logout;

public sealed class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Result<Guid>>
{
    private readonly IAuthenticationSession _session;
    private readonly IRefreshTokenService _refresh;

    public LogoutUserCommandHandler(IAuthenticationSession session, IRefreshTokenService refresh)
    {
        _session = session;
        _refresh = refresh;
    }

    public async Task<Result<Guid>> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        var token = _session.ReadRefreshToken();

        if (!string.IsNullOrEmpty(token))
        {
            await _refresh.RevokeUserAsync(token, cancellationToken);
        }

        _session.Clear();

        return Result<Guid>.Success(Guid.Empty);
    }
}
