using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.DTOs;
using MediatR;

namespace Brouj.Application.Features.Cities.Queries.GetById;

public sealed class GetCityByIdQuery : IRequest<Result<CityDto>>
{
    public Guid Id { get; init; }
}
