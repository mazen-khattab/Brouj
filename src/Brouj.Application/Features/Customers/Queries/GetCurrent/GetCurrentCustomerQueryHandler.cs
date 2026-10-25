using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Queries.GetCurrent;

public sealed class GetCurrentCustomerQueryHandler : IRequestHandler<GetCurrentCustomerQuery, Result<CurrentCustomerDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCurrentCustomerQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<CurrentCustomerDto>> Handle(
        GetCurrentCustomerQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.Customers.AsNoTracking().Where(customer => customer.Id == _currentUser.CustomerId!.Value)
            .Select(customer => new CurrentCustomerDto
            {
                Id = customer.Id,
                FName = customer.FName,
                LName = customer.LName,
                Email = customer.Email,
                Phone = customer.Phone,
                IdentityNumber = customer.IdentityNumber
            }).SingleAsync(cancellationToken);

        return Result<CurrentCustomerDto>.Success(dto);
    }
}
