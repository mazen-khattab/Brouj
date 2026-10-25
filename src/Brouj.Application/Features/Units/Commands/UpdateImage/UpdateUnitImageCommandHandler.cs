using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.UpdateImage;

public sealed class UpdateUnitImageCommandHandler : IRequestHandler<UpdateUnitImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateUnitImageCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateUnitImageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.UnitImages.SingleOrDefaultAsync(image => image.Id == request.Id && image.UnitId == request.UnitId, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        entity.AltText = request.AltText;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
