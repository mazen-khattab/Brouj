using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.DTOs;
using Brouj.Application.Features.StageTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Queries.GetById;

public sealed class GetStageTemplateByIdQueryHandler : IRequestHandler<GetStageTemplateByIdQuery, Result<StageTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetStageTemplateByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<StageTemplateDto>> Handle(
        GetStageTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.StageTemplates.AsNoTracking().Where(template => template.Id == request.Id)
            .Select(template => new StageTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Code = template.Code,
                IsActive = template.IsActive,
                Items = template.StageTemplateItems.OrderBy(mapping => mapping.SortOrder)
                    .Select(mapping => new StageTemplateItemDto
                    {
                        ItemTemplateId = mapping.ItemTemplateId,
                        Name = mapping.ItemTemplate.Name,
                        Code = mapping.ItemTemplate.Code,
                        IsActive = mapping.ItemTemplate.IsActive,
                        SortOrder = mapping.SortOrder
                    }).ToArray()
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<StageTemplateDto>.Failure(StageTemplateErrors.NotFound);
        }

        return Result<StageTemplateDto>.Success(dto);
    }
}
