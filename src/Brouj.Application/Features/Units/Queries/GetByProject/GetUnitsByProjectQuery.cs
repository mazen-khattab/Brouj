using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using MediatR;

namespace Brouj.Application.Features.Units.Queries.GetByProject;

public sealed class GetUnitsByProjectQuery : IRequest<Result<PaginationResult<UnitListDto>>>
{
    public Guid ProjectId { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}
