using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.TimePlans.Commands.Update;

public sealed class UpdateTimePlanCommandHandler : IRequestHandler<UpdateTimePlanCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public UpdateTimePlanCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(UpdateTimePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.TimePlans.SingleOrDefaultAsync(plan => plan.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(TimePlanErrors.NotFound);
        }

        entity.Name = request.Input.Name.Trim();
        entity.DeliveryDate = request.Input.DeliveryDate!.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
