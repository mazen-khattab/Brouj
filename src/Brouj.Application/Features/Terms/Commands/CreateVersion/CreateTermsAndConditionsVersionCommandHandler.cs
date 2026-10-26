using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Terms.Commands.CreateVersion;

public sealed class CreateTermsAndConditionsVersionCommandHandler : IRequestHandler<CreateTermsAndConditionsVersionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateTermsAndConditionsVersionCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(
        CreateTermsAndConditionsVersionCommand request,
        CancellationToken cancellationToken)
    {
        if (await _db.TermsAndConditions.AsNoTracking().AnyAsync(terms => terms.VersionNumber == request.Input.VersionNumber, cancellationToken))
        {
            return Result<Guid>.Failure(TermsErrors.Conflict("The version number is already used."));
        }

        var entity = new TermsAndConditions
        {
            Id = Guid.NewGuid(),
            Title = request.Input.Title.Trim(),
            Content = request.Input.Content,
            VersionNumber = request.Input.VersionNumber,
            EffectiveDate = request.Input.EffectiveDate,
            IsActive = false
        };

        _db.TermsAndConditions.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
