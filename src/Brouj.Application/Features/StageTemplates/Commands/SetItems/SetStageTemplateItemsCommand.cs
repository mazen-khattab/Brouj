using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.StageTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.StageTemplates.Commands.SetItems;

public sealed class SetStageTemplateItemsCommand : IRequest<Result<Guid>>, ITransactionalRequest, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }

    public IReadOnlyList<StageTemplateItemInput> Items { get; init; } = [];
}
