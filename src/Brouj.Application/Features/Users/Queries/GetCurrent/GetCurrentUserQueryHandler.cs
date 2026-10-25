using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.Features.Users.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Queries.GetCurrent;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Users.AsNoTracking().Where(user => user.Id == _currentUser.UserId!.Value)
            .Select(user => new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                DeletedAt = user.DeletedAt
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<UserDto>.Failure(UserErrors.NotFound);
        }

        return Result<UserDto>.Success(dto);
    }
}
