using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.AddImage;

public sealed class AddUnitImageCommandHandler : IRequestHandler<AddUnitImageCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly FileUpload _uploads;

    public AddUnitImageCommandHandler(IApplicationDbContext db, FileUpload uploads)
    {
        _db = db;
        _uploads = uploads;
    }

    public async Task<Result<Guid>> Handle(AddUnitImageCommand request, CancellationToken cancellationToken)
    {
        if (!await _db.BaseUnits.AsNoTracking().AnyAsync(parent => parent.Id == request.UnitId, cancellationToken))
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        var path = await _uploads.SaveAsync(request.File, cancellationToken);
        var entity = new UnitImage
        {
            Id = Guid.NewGuid(),
            UnitId = request.UnitId,
            ImagePath = path,
            AltText = request.AltText
        };

        try
        {
            _db.UnitImages.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _uploads.RemoveFailedAsync(path);

            throw;
        }

        return Result<Guid>.Success(entity.Id);
    }
}
