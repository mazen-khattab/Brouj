using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Users.Errors;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Commands.Update;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateUserCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Users.SingleOrDefaultAsync(user => user.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UserErrors.NotFound);
        }

        if (entity.DeletedAt is not null)
        {
            return Result<Guid>.Failure(UserErrors.Conflict("Restore the user before editing."));
        }

        if (entity.Role == UserRole.SuperAdmin && request.Input.Role != UserRole.SuperAdmin && (entity.Id == _currentUser.UserId!.Value || !await _db.Users.AsNoTracking().AnyAsync(user => user.Id != entity.Id && user.DeletedAt == null && user.Role == UserRole.SuperAdmin, cancellationToken)))
        {
            return Result<Guid>.Failure(UserErrors.Conflict("The current or last active SuperAdmin cannot be demoted."));
        }

        var email = InputRules.NormalizeEmail(request.Input.Email);

        if (await _db.Users.AsNoTracking().AnyAsync(user => user.Id != entity.Id && (user.Email == email || user.Phone == request.Input.Phone), cancellationToken))
        {
            return Result<Guid>.Failure(UserErrors.Conflict("The supplied account details cannot be used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Email = email;
        entity.Phone = request.Input.Phone;
        entity.Role = request.Input.Role;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
