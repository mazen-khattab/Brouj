using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;

namespace Brouj.Application.Features.Terms.Queries.GetVersions;

public sealed class GetTermsAndConditionsVersionsQuery : IRequest<Result<PaginationResult<TermsListDto>>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
