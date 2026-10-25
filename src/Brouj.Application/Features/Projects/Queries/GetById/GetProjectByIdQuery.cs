using Brouj.Application.Common.Models;
using Brouj.Application.Features.Projects.DTOs;
using MediatR;

namespace Brouj.Application.Features.Projects.Queries.GetById;

public sealed class GetProjectByIdQuery : IRequest<Result<ProjectDto>>
{
    public Guid Id { get; init; }
}
