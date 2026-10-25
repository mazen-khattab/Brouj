using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Queries.GetByProject;

public sealed class GetProjectTimePlanQueryHandler : IRequestHandler<GetProjectTimePlanQuery, Result<TimePlanDto>>
{
    private readonly IApplicationDbContext _db;

    public GetProjectTimePlanQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TimePlanDto>> Handle(GetProjectTimePlanQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.TimePlans.AsNoTracking().Where(plan => plan.ProjectId == request.ProjectId)
            .Select(plan => new TimePlanDto
            {
                Id = plan.Id,
                ProjectId = plan.ProjectId,
                Name = plan.Name,
                DeliveryDate = plan.DeliveryDate,
                Stages = plan.TimePlanStages.OrderBy(stage => stage.SortOrder).Select(stage => new TimePlanStageDto
                {
                    Id = stage.Id,
                    Name = stage.Name,
                    SortOrder = stage.SortOrder,
                    DeliveryDate = stage.DeliveryDate,
                    Items = stage.PlanStageItems.OrderBy(item => item.SortOrder).Select(item => new PlanStageItemDto
                    {
                        Id = item.Id,
                        Name = item.Name,
                        SortOrder = item.SortOrder,
                        Status = item.Status,
                        DueDate = item.DueDate,
                        ConfirmedDate = item.ConfirmedDate,
                        Images = item.StageItemImages.OrderBy(image => image.SortOrder).Select(image => new StageItemImageDto
                        {
                            Id = image.Id,
                            ImagePath = image.ImagePath,
                            Description = image.Description,
                            SortOrder = image.SortOrder
                        }).ToArray()
                    }).ToArray()
                }).ToArray()
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<TimePlanDto>.Failure(TimePlanErrors.NotFound);
        }

        return Result<TimePlanDto>.Success(dto);
    }
}
