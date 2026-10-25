using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Users.DTOs;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.Update;

public sealed class UpdateUserCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.SuperAdmin;

    public Guid Id { get; init; }

    public UpdateUserInput Input { get; init; } = new();
}
