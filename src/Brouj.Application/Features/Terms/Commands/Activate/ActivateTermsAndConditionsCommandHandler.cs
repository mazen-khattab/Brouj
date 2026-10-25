using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Terms.Commands.Activate;

public sealed class ActivateTermsAndConditionsCommandHandler : IRequestHandler<ActivateTermsAndConditionsCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public ActivateTermsAndConditionsCommandHandler(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        ActivateTermsAndConditionsCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.TermsAndConditions.SingleOrDefaultAsync(terms => terms.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TermsErrors.NotFound);
        }

        if (entity.EffectiveDate > _clock.GetUtcNow())
        {
            return Result<Guid>.Failure(TermsErrors.Conflict("A future version cannot be activated before its effective date."));
        }

        var active = await _db.TermsAndConditions.Where(terms => terms.IsActive && terms.Id != entity.Id)
            .ToListAsync(cancellationToken);

        foreach (var other in active)
        {
            other.IsActive = false;
        }

        entity.IsActive = true;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
