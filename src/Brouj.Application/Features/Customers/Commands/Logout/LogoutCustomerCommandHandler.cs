using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.Logout;

public sealed class LogoutCustomerCommandHandler : IRequestHandler<LogoutCustomerCommand, Result<Guid>>
{
    private readonly IAuthenticationSession _session;
    private readonly IRefreshTokenService _refresh;

    public LogoutCustomerCommandHandler(IAuthenticationSession session, IRefreshTokenService refresh)
    {
        _session = session;
        _refresh = refresh;
    }

    public async Task<Result<Guid>> Handle(LogoutCustomerCommand request, CancellationToken cancellationToken)
    {
        var token = _session.ReadRefreshToken();

        if (!string.IsNullOrEmpty(token))
        {
            await _refresh.RevokeCustomerAsync(token, cancellationToken);
        }

        _session.Clear();

        return Result<Guid>.Success(Guid.Empty);
    }
}
