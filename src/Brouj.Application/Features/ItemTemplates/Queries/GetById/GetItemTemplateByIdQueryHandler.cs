using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.DTOs;
using Brouj.Application.Features.ItemTemplates.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ItemTemplates.Queries.GetById;

public sealed class GetItemTemplateByIdQueryHandler : IRequestHandler<GetItemTemplateByIdQuery, Result<ItemTemplateDto>>
{
    private readonly IApplicationDbContext _db;

    public GetItemTemplateByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ItemTemplateDto>> Handle(
        GetItemTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.ItemTemplates.AsNoTracking().Where(template => template.Id == request.Id)
            .Select(template => new ItemTemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Code = template.Code,
                IsActive = template.IsActive
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<ItemTemplateDto>.Failure(ItemTemplateErrors.NotFound);
        }

        return Result<ItemTemplateDto>.Success(dto);
    }
}
