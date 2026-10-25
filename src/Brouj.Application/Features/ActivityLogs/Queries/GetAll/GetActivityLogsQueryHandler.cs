using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.ActivityLogs.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetAll;

public sealed class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, Result<PaginationResult<ActivityLogDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetActivityLogsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<ActivityLogDto>>> Handle(
        GetActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.ActivityLogs.AsNoTracking();

        if (request.UserId is not null)
        {
            query = query.Where(log => log.UserId == request.UserId);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(log => log.EntityType == request.EntityType.Trim());
        }

        if (request.Action is not null)
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (request.DateFrom is not null)
        {
            query = query.Where(log => log.CreatedAt >= request.DateFrom);
        }

        if (request.DateTo is not null)
        {
            query = query.Where(log => log.CreatedAt <= request.DateTo);
        }

        // Raw OldData/NewData are intentionally excluded: they are not safe screen DTOs.
        var page = await Paging.ToPageAsync(query.OrderByDescending(log => log.CreatedAt).ThenBy(log => log.Id).Select(log => new ActivityLogDto
        {
            Id = log.Id,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            UserId = log.UserId,
            Action = log.Action,
            CreatedAt = log.CreatedAt
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<ActivityLogDto>>.Success(page);
    }
}
