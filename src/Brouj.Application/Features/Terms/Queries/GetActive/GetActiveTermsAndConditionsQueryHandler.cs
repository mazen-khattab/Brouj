using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using Brouj.Application.Features.Terms.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Terms.Queries.GetActive;

public sealed class GetActiveTermsAndConditionsQueryHandler : IRequestHandler<GetActiveTermsAndConditionsQuery, Result<TermsDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public GetActiveTermsAndConditionsQueryHandler(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TermsDto>> Handle(
        GetActiveTermsAndConditionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.TermsAndConditions.AsNoTracking();

        var now = _clock.GetUtcNow();
        query = query.Where(terms => terms.IsActive && terms.EffectiveDate <= now);
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
