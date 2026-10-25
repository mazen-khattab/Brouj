using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ActivityLogs.DTOs;
using Brouj.Application.Features.ActivityLogs.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetById;

public sealed class GetActivityLogByIdQueryHandler : IRequestHandler<GetActivityLogByIdQuery, Result<ActivityLogDto>>
{
    private readonly IApplicationDbContext _db;

    public GetActivityLogByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ActivityLogDto>> Handle(
        GetActivityLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.ActivityLogs.AsNoTracking().Where(log => log.Id == request.Id)
            .Select(log => new ActivityLogDto
            {
                Id = log.Id,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                UserId = log.UserId,
                Action = log.Action,
                CreatedAt = log.CreatedAt
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<ActivityLogDto>.Failure(ActivityLogErrors.NotFound);
        }

        return Result<ActivityLogDto>.Success(dto);
    }
}
