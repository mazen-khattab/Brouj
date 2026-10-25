using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Services;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.Errors;
using Brouj.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Commands.Register;

public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<AuthSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenService _refresh;
    private readonly SessionResponse _response;

    public RegisterCustomerCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher hasher,
        IRefreshTokenService refresh,
        SessionResponse response)
    {
        _db = db;
        _hasher = hasher;
        _refresh = refresh;
        _response = response;
    }

    public async Task<Result<AuthSessionDto>> Handle(
        RegisterCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var email = InputRules.NormalizeEmail(request.Input.Email);

        if (await _db.Customers.AsNoTracking().AnyAsync(customer =>
            customer.Id != id && (customer.Email == email || customer.Phone == request.Input.Phone ||
                customer.IdentityNumber == request.Input.IdentityNumber), cancellationToken))
        {
            return Result<AuthSessionDto>.Failure(CustomerErrors.Conflict("The supplied account details cannot be used."));
        }

        var entity = new Customer
        {
            Id = id,
            FName = request.Input.FName.Trim(),
            LName = request.Input.LName.Trim(),
            Phone = request.Input.Phone,
            IdentityNumber = request.Input.IdentityNumber,
            Email = email,
            HashPassword = _hasher.Hash(request.Input.Password)
        };

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // RefreshTokenService uses an isolated context; the account must be committed before issuing its session.
        var credentials = await _refresh.CreateCustomerAsync(id, cancellationToken);

        return _response.Write(credentials);
    }
}
