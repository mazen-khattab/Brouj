using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Common.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Features.Users.Commands.Login;

public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly LoginPasswordVerifier _passwords;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenService _refresh;
    private readonly SessionResponse _response;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IApplicationDbContext db,
        LoginPasswordVerifier passwords,
        IPasswordHasher hasher,
        IRefreshTokenService refresh,
        SessionResponse response,
        ILogger<LoginUserCommandHandler> logger)
    {
        _db = db;
        _passwords = passwords;
        _hasher = hasher;
        _refresh = refresh;
        _response = response;
        _logger = logger;
    }

    public async Task<Result<AuthSessionDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var email = InputRules.NormalizeEmail(request.Input.Email);

        _logger.LogInformation("Processing login attempt for email: {Email}", email);

        var entity = await _db.Users.SingleOrDefaultAsync(account => account.Email == email, cancellationToken);
        var activeHash = entity?.DeletedAt is null ? entity?.HashPassword : null;
        var verified = _passwords.Verify(request.Input.Password, activeHash);

        if (!verified || entity is null || !Enum.IsDefined(entity.Role))
        {
            _logger.LogWarning("Login failed for email: {Email}. Invalid credentials, account deleted, or undefined role.", email);

            return Result<AuthSessionDto>.Failure(ApplicationErrors.InvalidCredentials);
        }

        if (_hasher.NeedsRehash(entity.HashPassword))
        {
            _logger.LogInformation("Upgrading password hash for user: {UserId}", entity.Id);

            entity.HashPassword = _hasher.Hash(request.Input.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var credentials = await _refresh.CreateUserAsync(entity.Id, cancellationToken);

        _logger.LogInformation("User {UserId} successfully logged in.", entity.Id);

        return _response.Write(credentials);
    }
}