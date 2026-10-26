using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.DTOs;
using MediatR;

namespace Brouj.Application.Features.Users.Queries.GetById;

public sealed class GetUserByIdQuery : IRequest<Result<UserDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.SuperAdmin;

    public Guid Id { get; init; }
}
