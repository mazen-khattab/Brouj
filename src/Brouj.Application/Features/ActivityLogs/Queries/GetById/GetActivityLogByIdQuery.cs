using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ActivityLogs.DTOs;
using MediatR;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetById;

public sealed class GetActivityLogByIdQuery : IRequest<Result<ActivityLogDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
