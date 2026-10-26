using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Common.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.Login;

public sealed class LoginCustomerCommandHandler : IRequestHandler<LoginCustomerCommand, Result<AuthSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly LoginPasswordVerifier _passwords;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenService _refresh;
    private readonly SessionResponse _response;

    public LoginCustomerCommandHandler(
        IApplicationDbContext db,
        LoginPasswordVerifier passwords,
        IPasswordHasher hasher,
        IRefreshTokenService refresh,
        SessionResponse response)
    {
        _db = db;
        _passwords = passwords;
        _hasher = hasher;
        _refresh = refresh;
        _response = response;
    }

    public async Task<Result<AuthSessionDto>> Handle(LoginCustomerCommand request, CancellationToken cancellationToken)
    {
        var email = InputRules.NormalizeEmail(request.Input.Email);
        var entity = await _db.Customers.SingleOrDefaultAsync(account => account.Email == email, cancellationToken);
        var activeHash = entity?.DeletedAt is null ? entity?.HashPassword : null;
        var verified = _passwords.Verify(request.Input.Password, activeHash);

        if (!verified || entity is null)
        {
            return Result<AuthSessionDto>.Failure(ApplicationErrors.InvalidCredentials);
        }

        if (_hasher.NeedsRehash(entity.HashPassword))
        {
            entity.HashPassword = _hasher.Hash(request.Input.Password);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var credentials = await _refresh.CreateCustomerAsync(entity.Id, cancellationToken);

        return _response.Write(credentials);
    }
}
