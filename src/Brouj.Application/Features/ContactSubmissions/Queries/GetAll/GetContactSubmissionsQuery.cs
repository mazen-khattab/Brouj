using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ContactSubmissions.DTOs;
using MediatR;

namespace Brouj.Application.Features.ContactSubmissions.Queries.GetAll;

public sealed class GetContactSubmissionsQuery : IRequest<Result<PaginationResult<ContactSubmissionDto>>>, IRequireAccess
{
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
