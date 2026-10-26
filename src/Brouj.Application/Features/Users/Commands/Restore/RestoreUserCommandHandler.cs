using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Commands.Restore;

public sealed class RestoreUserCommandHandler : IRequestHandler<RestoreUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public RestoreUserCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(RestoreUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users.SingleOrDefaultAsync(user => user.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UserErrors.NotFound);
        }

        // Legacy data may have reused a deleted account's values through the filtered unique indexes.
        var collision = await _db.Users.AsNoTracking().AnyAsync(account =>
            account.Id != entity.Id && account.DeletedAt == null &&
            (account.Email == entity.Email || account.Phone == entity.Phone), cancellationToken);

        if (collision)
        {
            return Result<Guid>.Failure(UserErrors.Conflict("Account details are already in use."));
        }

        entity.DeletedAt = null;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
