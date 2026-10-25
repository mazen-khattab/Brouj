using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using MediatR;

namespace Brouj.Application.Features.Units.Queries.GetById;

public sealed class GetUnitByIdQuery : IRequest<Result<UnitDto>>
{
    public Guid ProjectId { get; init; }

    public Guid Id { get; init; }
}
