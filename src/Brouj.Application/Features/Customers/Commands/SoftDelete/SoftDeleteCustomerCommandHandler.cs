using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.SoftDelete;

public sealed class SoftDeleteCustomerCommandHandler : IRequestHandler<SoftDeleteCustomerCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public SoftDeleteCustomerCommandHandler(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(SoftDeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Customers.SingleOrDefaultAsync(customer => customer.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(CustomerErrors.NotFound);
        }

        entity.DeletedAt = _clock.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
