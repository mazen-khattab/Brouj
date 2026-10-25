using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Users.Commands.ChangePassword;

public sealed class ChangeUserPasswordCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public PasswordChangeInput Input { get; init; } = new();
}
