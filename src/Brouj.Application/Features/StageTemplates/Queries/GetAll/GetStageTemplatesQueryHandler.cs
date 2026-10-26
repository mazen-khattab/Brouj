using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.StageTemplates.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.StageTemplates.Queries.GetAll;

public sealed class GetStageTemplatesQueryHandler : IRequestHandler<GetStageTemplatesQuery, Result<PaginationResult<StageTemplateListDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetStageTemplatesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<StageTemplateListDto>>> Handle(
        GetStageTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.StageTemplates.AsNoTracking();

        if (request.IsActive is not null)
        {
            query = query.Where(template => template.IsActive == request.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(template => template.Name.Contains(request.Search.Trim()) || template.Code.Contains(request.Search.Trim()));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(template => template.Name).ThenBy(template => template.Id).Select(template => new StageTemplateListDto { Id = template.Id, Name = template.Name, Code = template.Code, IsActive = template.IsActive }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<StageTemplateListDto>>.Success(page);
    }
}
