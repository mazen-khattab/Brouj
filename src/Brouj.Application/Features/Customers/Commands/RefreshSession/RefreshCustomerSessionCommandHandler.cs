using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using MediatR;

namespace Brouj.Application.Features.Customers.Commands.RefreshSession;

public sealed class RefreshCustomerSessionCommandHandler : IRequestHandler<RefreshCustomerSessionCommand, Result<AuthSessionDto>>
{
    private readonly IAuthenticationSession _session;
    private readonly IRefreshTokenService _refresh;
    private readonly SessionResponse _response;

    public RefreshCustomerSessionCommandHandler(
        IAuthenticationSession session,
        IRefreshTokenService refresh,
        SessionResponse response)
    {
        _session = session;
        _refresh = refresh;
        _response = response;
    }

    public async Task<Result<AuthSessionDto>> Handle(
        RefreshCustomerSessionCommand request,
        CancellationToken cancellationToken)
    {
        var token = _session.ReadRefreshToken();

        if (string.IsNullOrEmpty(token))
        {
            return Result<AuthSessionDto>.Failure(ApplicationErrors.InvalidCredentials);
        }

        var credentials = await _refresh.RotateCustomerAsync(token, cancellationToken);

        return _response.Write(credentials);
    }
}
