using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Customers.Queries.GetAll;

public sealed class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, Result<PaginationResult<CustomerDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetCustomersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginationResult<CustomerDto>>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Customers.AsNoTracking();

        if (request.IsDeleted is not null)
        {
            query = query.Where(customer => (customer.DeletedAt != null) == request.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(customer => customer.FName.Contains(search) || customer.LName.Contains(search) ||
                customer.Email.Contains(search));
        }

        var page = await Paging.ToPageAsync(query.OrderBy(customer => customer.FName).ThenBy(customer => customer.Id).Select(customer => new CustomerDto
        {
            Id = customer.Id,
            FName = customer.FName,
            LName = customer.LName,
            Email = customer.Email,
            Phone = customer.Phone,
            DeletedAt = customer.DeletedAt
        }), request.PageNumber, request.PageSize, cancellationToken);

        return Result<PaginationResult<CustomerDto>>.Success(page);
    }
}
