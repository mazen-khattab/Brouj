using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Queries.GetByProject;

public sealed class GetProjectTimePlanQuery : IRequest<Result<TimePlanDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Member;

    public Guid ProjectId { get; init; }
}
