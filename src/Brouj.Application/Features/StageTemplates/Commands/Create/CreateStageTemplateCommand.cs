using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.StageTemplates.Commands.Create;

public sealed class CreateStageTemplateCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public StageTemplateInput Input { get; init; } = new();
}
