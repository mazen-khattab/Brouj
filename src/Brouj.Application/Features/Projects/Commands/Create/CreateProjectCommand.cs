using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using MediatR;

namespace Brouj.Application.Features.Projects.Commands.Create;

[InvalidatesCache("catalogue")]
public sealed class CreateProjectCommand : IRequest<Result<Guid>>, IRequireAccess, ITransactionalRequest
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public ProjectInput Input { get; init; } = new();
}
