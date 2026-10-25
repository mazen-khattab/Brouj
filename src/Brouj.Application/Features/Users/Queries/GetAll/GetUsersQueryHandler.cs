using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Users.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Users.Queries.GetAll;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginationResult<UserDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (request.IsDeleted is not null)
        {
            query = query.Where(user => (user.DeletedAt != null) == request.IsDeleted);
        }

        if (request.Role is not null)
        {
            query = query.Where(user => user.Role == request.Role);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(user => user.Name.Contains(search) || user.Email.Contains(search));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(user => user.Name).ThenBy(user => user.Id).Select(user => new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            DeletedAt = user.DeletedAt
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<UserDto>>.Success(page);
    }
}
