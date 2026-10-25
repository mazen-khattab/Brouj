using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.UpdateProfile;

public sealed class UpdateCustomerProfileCommandHandler : IRequestHandler<UpdateCustomerProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateCustomerProfileCommandHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateCustomerProfileCommand request, CancellationToken cancellationToken)
    {
        var id = _currentUser.CustomerId!.Value;
        var entity = await _db.Customers.SingleAsync(customer => customer.Id == id, cancellationToken);
        var email = InputRules.NormalizeEmail(request.Input.Email);

        if (await _db.Customers.AsNoTracking().AnyAsync(customer =>
            customer.Id != id && (customer.Email == email || customer.Phone == request.Input.Phone ||
                customer.IdentityNumber == request.Input.IdentityNumber), cancellationToken))
        {
            return Result<Guid>.Failure(CustomerErrors.Conflict("The supplied account details cannot be used."));
        }

        entity.FName = request.Input.FName.Trim();
        entity.LName = request.Input.LName.Trim();
        entity.Phone = request.Input.Phone;
        entity.IdentityNumber = request.Input.IdentityNumber;
        entity.Email = email;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
