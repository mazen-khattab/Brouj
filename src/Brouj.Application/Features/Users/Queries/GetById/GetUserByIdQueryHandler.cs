using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.Features.Users.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Queries.GetById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUserByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Users.AsNoTracking().Where(user => user.Id == request.Id)
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
