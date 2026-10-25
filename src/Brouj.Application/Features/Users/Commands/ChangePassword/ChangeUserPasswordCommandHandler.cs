using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Commands.ChangePassword;

public sealed class ChangeUserPasswordCommandHandler : IRequestHandler<ChangeUserPasswordCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _hasher;

    public ChangeUserPasswordCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    public async Task<Result<Guid>> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users.SingleAsync(account => account.Id == _currentUser.UserId!.Value, cancellationToken);

        if (!_hasher.Verify(request.Input.CurrentPassword, entity.HashPassword))
        {
            return Result<Guid>.Failure(ApplicationErrors.InvalidCredentials);
        }

        // Keep the existing session, as approved; never trim or otherwise transform a password.
        entity.HashPassword = _hasher.Hash(request.Input.NewPassword);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
