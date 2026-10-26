using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Customers.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.Restore;

public sealed class RestoreCustomerCommandHandler : IRequestHandler<RestoreCustomerCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public RestoreCustomerCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(RestoreCustomerCommand request, CancellationToken cancellationToken)
    {
        var entity = await _db.Customers.SingleOrDefaultAsync(customer => customer.Id == request.Id, cancellationToken);

        if (entity is null)
        {
            return Result<Guid>.Failure(CustomerErrors.NotFound);
        }

        // Legacy data may have reused a deleted account's values through the filtered unique indexes.
        var collision = await _db.Customers.AsNoTracking().AnyAsync(account =>
            account.Id != entity.Id && account.DeletedAt == null &&
            (account.Email == entity.Email || account.Phone == entity.Phone || account.IdentityNumber == entity.IdentityNumber), cancellationToken);

        if (collision)
        {
            return Result<Guid>.Failure(CustomerErrors.Conflict("Account details are already in use."));
        }

        entity.DeletedAt = null;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
