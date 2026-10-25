using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Units.Commands.UpdateImage;

[InvalidatesCache("catalogue")]
public sealed class UpdateUnitImageCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid UnitId { get; init; }

    public Guid Id { get; init; }

    public string? AltText { get; init; }
}
