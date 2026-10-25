using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.DTOs;
using MediatR;

namespace Brouj.Application.Features.Users.Queries.GetCurrent;

public sealed class GetCurrentUserQuery : IRequest<Result<UserDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

}
