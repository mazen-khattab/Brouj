using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;

namespace Brouj.Application.Features.Areas.Commands.Update;

[InvalidatesCache("catalogue")]
public sealed class UpdateAreaCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }

    public AreaInput Input { get; init; } = new();
}
