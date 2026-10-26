using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Users.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Commands.Create;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var entity = new User { Id = Guid.NewGuid() };
        var email = InputRules.NormalizeEmail(request.Input.Email);

        if (await _db.Users.AsNoTracking().AnyAsync(user => user.Id != entity.Id && (user.Email == email || user.Phone == request.Input.Phone), cancellationToken))
        {
            return Result<Guid>.Failure(UserErrors.Conflict("The supplied account details cannot be used."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.Email = email;
        entity.Phone = request.Input.Phone;
        entity.Role = request.Input.Role;
        entity.HashPassword = _hasher.Hash(request.Input.Password);

        _db.Users.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
