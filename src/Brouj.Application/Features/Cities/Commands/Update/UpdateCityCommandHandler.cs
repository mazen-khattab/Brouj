using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Cities.Commands.Update;

public sealed class UpdateCityCommandHandler : IRequestHandler<UpdateCityCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateCityCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Cities.SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(CityErrors.NotFound);
        }

        entity.Name = request.Input.Name.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
