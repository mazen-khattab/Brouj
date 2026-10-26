using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Units.Commands.Delete;

[InvalidatesCache("catalogue")]
public sealed class DeleteUnitCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid ProjectId { get; init; }

    public Guid Id { get; init; }
}
