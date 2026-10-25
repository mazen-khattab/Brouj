using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using MediatR;

namespace Brouj.Application.Features.Projects.Commands.ReplaceAmenities;

[InvalidatesCache("catalogue")]
public sealed class ReplaceProjectAmenitiesCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid ProjectId { get; init; }

    public IReadOnlyList<AmenityDto> Amenities { get; init; } = [];
}
