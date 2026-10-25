using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Terms.Queries.GetVersions;

public sealed class GetTermsAndConditionsVersionsQueryHandler : IRequestHandler<GetTermsAndConditionsVersionsQuery, Result<PaginationResult<TermsListDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetTermsAndConditionsVersionsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<TermsListDto>>> Handle(
        GetTermsAndConditionsVersionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await Paging.ToPageAsync(_db.TermsAndConditions.AsNoTracking().OrderByDescending(terms => terms.VersionNumber)
            .Select(terms => new TermsListDto
            {
                Id = terms.Id,
                Title = terms.Title,
                VersionNumber = terms.VersionNumber,
                EffectiveDate = terms.EffectiveDate,
                IsActive = terms.IsActive
            }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<TermsListDto>>.Success(page);
    }
}
