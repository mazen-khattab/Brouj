using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.DTOs;
using MediatR;

namespace Brouj.Application.Features.Cities.Queries.GetAll;

public sealed class GetCitiesQuery : IRequest<Result<PaginationResult<CityDto>>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}
