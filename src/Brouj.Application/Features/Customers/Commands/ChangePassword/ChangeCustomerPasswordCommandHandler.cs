using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.ChangePassword;

public sealed class ChangeCustomerPasswordCommandHandler : IRequestHandler<ChangeCustomerPasswordCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _hasher;

    public ChangeCustomerPasswordCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    public async Task<Result<Guid>> Handle(ChangeCustomerPasswordCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Customers.SingleAsync(account => account.Id == _currentUser.CustomerId!.Value, cancellationToken);

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
