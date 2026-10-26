using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Domain.Entities;
using MediatR;

namespace Brouj.Application.Features.Cities.Commands.Create;

public sealed class CreateCityCommandHandler : IRequestHandler<CreateCityCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateCityCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        var entity = new City { Id = Guid.NewGuid() };

        entity.Name = request.Input.Name.Trim();

        _db.Cities.Add(entity);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
