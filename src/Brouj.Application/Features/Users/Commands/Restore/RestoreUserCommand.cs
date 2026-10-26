using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Restore;

public sealed class RestoreUserCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.SuperAdmin;

    public Guid Id { get; init; }
}
