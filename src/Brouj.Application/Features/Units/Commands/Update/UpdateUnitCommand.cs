using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using MediatR;

namespace Brouj.Application.Features.Units.Commands.Update;

[InvalidatesCache("catalogue")]
public sealed class UpdateUnitCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid ProjectId { get; init; }

    public Guid Id { get; init; }

    public UnitInput Input { get; init; } = new();
}
