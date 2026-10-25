using Brouj.Application.Common.Models;
using Brouj.Application.Features.Areas.DTOs;
using MediatR;

namespace Brouj.Application.Features.Areas.Queries.GetById;

public sealed class GetAreaByIdQuery : IRequest<Result<AreaDto>>
{
    public Guid Id { get; init; }
}
