using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;

namespace Brouj.Application.Features.Terms.Queries.GetById;

public sealed class GetTermsAndConditionsByIdQuery : IRequest<Result<TermsDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
