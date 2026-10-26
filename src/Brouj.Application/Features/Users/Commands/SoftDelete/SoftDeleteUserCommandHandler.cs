using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Commands.SoftDelete;

public sealed class SoftDeleteUserCommandHandler : IRequestHandler<SoftDeleteUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public SoftDeleteUserCommandHandler(IApplicationDbContext db, ICurrentUser currentUser, TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(SoftDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users.SingleOrDefaultAsync(user => user.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UserErrors.NotFound);
        }

        if (entity.Id == _currentUser.UserId!.Value || (entity.DeletedAt == null && entity.Role == UserRole.SuperAdmin && !await _db.Users.AsNoTracking().AnyAsync(user => user.Id != entity.Id && user.DeletedAt == null && user.Role == UserRole.SuperAdmin, cancellationToken)))
        {
            return Result<Guid>.Failure(UserErrors.Conflict("The current or last active SuperAdmin cannot be deleted."));
        }

        entity.DeletedAt = _clock.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
