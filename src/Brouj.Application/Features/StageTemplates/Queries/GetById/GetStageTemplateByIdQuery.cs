using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.StageTemplates.Queries.GetById;

public sealed class GetStageTemplateByIdQuery : IRequest<Result<StageTemplateDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
