using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.Customers.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Queries.GetById;

public sealed class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCustomerByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Customers.AsNoTracking().Where(customer => customer.Id == request.Id)
            .Select(customer => new CustomerDto
            {
                Id = customer.Id,
                FName = customer.FName,
                LName = customer.LName,
                Email = customer.Email,
                Phone = customer.Phone,
                DeletedAt = customer.DeletedAt
            }).SingleOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return Result<CustomerDto>.Failure(CustomerErrors.NotFound);
        }

        return Result<CustomerDto>.Success(dto);
    }
}
