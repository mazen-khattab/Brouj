using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStageItemImage;

[InvalidatesCache("time-plans")]
public sealed class UpdateStageItemImageCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid ItemId { get; init; }

    public Guid Id { get; init; }

    public string? Description { get; init; }
}
