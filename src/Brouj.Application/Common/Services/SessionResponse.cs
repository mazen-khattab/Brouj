using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;

namespace Brouj.Application.Common.Services;

public sealed class SessionResponse
{
    private readonly IAuthenticationSession _session;
    private readonly IJwtTokenService _jwt;

    public SessionResponse(IAuthenticationSession session, IJwtTokenService jwt)
    {
        _session = session;
        _jwt = jwt;
    }

    public Result<AuthSessionDto> Write(RefreshTokenResult? credentials)
    {
        if (credentials is null)
        {
            return Result<AuthSessionDto>.Failure(ApplicationErrors.InvalidCredentials);
        }

        var principal = _jwt.ValidateAccessToken(credentials.AccessToken);
        var subject = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subject, out var actorId))
        {
            throw new InvalidOperationException("Session subject is invalid.");
        }

        _session.Write(credentials);

        return Result<AuthSessionDto>.Success(new AuthSessionDto
        {
            ActorId = actorId,
            ExpiresAt = credentials.ExpiresAt
        });
    }
}
