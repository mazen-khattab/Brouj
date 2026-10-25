using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using MediatR;

namespace Brouj.Application.Features.Projects.Commands.Update;

[InvalidatesCache("catalogue")]
public sealed class UpdateProjectCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }

    public ProjectInput Input { get; init; } = new();
}
