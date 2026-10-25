using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Areas.Commands.Create;

public sealed class CreateAreaCommandHandler : IRequestHandler<CreateAreaCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateAreaCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateAreaCommand request, CancellationToken cancellationToken)
    {
        var entity = new Area { Id = Guid.NewGuid() };

        if (!await _db.Cities.AsNoTracking().AnyAsync(city => city.Id == request.Input.CityId, cancellationToken))
        {
            return Result<Guid>.Failure(AreaErrors.Conflict("The selected city does not exist."));
        }

        entity.Name = request.Input.Name.Trim();
        entity.CityId = request.Input.CityId;

        _db.Areas.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
