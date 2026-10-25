using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.Create;

[InvalidatesCache("time-plans")]
public sealed class CreateTimePlanCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid ProjectId { get; init; }

    public TimePlanInput Input { get; init; } = new();

    public IReadOnlyList<StageDraftInput> Stages { get; init; } = [];
}
