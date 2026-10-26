using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ActivityLogs.DTOs;
using Brouj.Domain.Enums;
using MediatR;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetAll;

public sealed class GetActivityLogsQuery : IRequest<Result<PaginationResult<ActivityLogDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? UserId { get; init; }

    public string? EntityType { get; init; }

    public ActivityAction? Action { get; init; }

    public DateTimeOffset? DateFrom { get; init; }

    public DateTimeOffset? DateTo { get; init; }
}
