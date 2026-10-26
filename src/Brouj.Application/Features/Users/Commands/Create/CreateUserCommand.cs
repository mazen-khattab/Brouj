using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.DTOs;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Create;

public sealed class CreateUserCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.SuperAdmin;

    public CreateUserInput Input { get; init; } = new();
}
