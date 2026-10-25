using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.Users.Queries.GetAll;

public sealed class GetUsersQuery : IRequest<Result<PaginationResult<UserDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.SuperAdmin;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public bool? IsDeleted { get; init; }

    public UserRole? Role { get; init; }
}
