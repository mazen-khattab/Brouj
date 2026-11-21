using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.ContactSubmissions.Queries.GetAll;

public sealed class GetContactSubmissionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetContactSubmissionsQuery, Result<PaginationResult<ContactSubmissionDto>>>
{
    public async Task<Result<PaginationResult<ContactSubmissionDto>>> Handle(
        GetContactSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.ContactSubmissions.AsNoTracking()
            .OrderByDescending(submission => submission.CreatedAt)
            .ThenBy(submission => submission.Id)
            .Select(submission => new ContactSubmissionDto
            {
                Id = submission.Id,
                Name = submission.Name,
                Phone = submission.Phone,
                Inquiry = submission.Inquiry,
                CreatedAt = submission.CreatedAt
            });

        var page = await Paging.ToPageAsync(query, request.PageNumber, request.PageSize, cancellationToken);
        return Result<PaginationResult<ContactSubmissionDto>>.Success(page);
    }
}
