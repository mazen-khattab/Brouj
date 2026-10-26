using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Commands.Update;

public sealed class UpdateAreaCommandHandler : IRequestHandler<UpdateAreaCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateAreaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Areas.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(AreaErrors.NotFound);
        }

        if (!await _db.Cities.AsNoTracking().AnyAsync(city => city.Id == request.Input.CityId, cancellationToken))
        {
            return Result<Guid>.Failure(AreaErrors.Conflict("The selected city does not exist."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.CityId = request.Input.CityId;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
