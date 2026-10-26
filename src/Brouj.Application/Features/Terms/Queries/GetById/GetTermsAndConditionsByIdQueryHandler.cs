using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.Terms.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Terms.Queries.GetById;

public sealed class GetTermsAndConditionsByIdQueryHandler : IRequestHandler<GetTermsAndConditionsByIdQuery, Result<TermsDto>>
{
    private readonly IApplicationDbContext _db;

    public GetTermsAndConditionsByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TermsDto>> Handle(
        GetTermsAndConditionsByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.TermsAndConditions.AsNoTracking();

        query = query.Where(terms => terms.Id == request.Id);
        var dto = await query.OrderByDescending(terms => terms.EffectiveDate).ThenByDescending(terms => terms.VersionNumber)
            .Select(terms => new TermsDto
            {
                Id = terms.Id,
                Title = terms.Title,
                Content = terms.Content,
                VersionNumber = terms.VersionNumber,
                EffectiveDate = terms.EffectiveDate,
                IsActive = terms.IsActive
            }).FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<TermsDto>.Failure(TermsErrors.NotFound);
        }

        return Result<TermsDto>.Success(dto);
    }
}
