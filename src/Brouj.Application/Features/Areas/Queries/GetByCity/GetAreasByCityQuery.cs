using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;

namespace Brouj.Application.Features.Areas.Queries.GetByCity;

public sealed class GetAreasByCityQuery : IRequest<Result<PaginationResult<AreaListDto>>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public Guid CityId { get; init; }
}
