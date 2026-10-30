using Brouj.Application.Common.Models;
using Brouj.Application.Features.TimePlans.DTOs;
using MediatR;

namespace Brouj.Application.Features.TimePlans.Queries.GetByProject;

public sealed class GetProjectTimePlanQuery : IRequest<Result<TimePlanDto>>
{
    public Guid ProjectId { get; init; }
}
