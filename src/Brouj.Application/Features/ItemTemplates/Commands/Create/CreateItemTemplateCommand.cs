using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.ItemTemplates.Commands.Create;

public sealed class CreateItemTemplateCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public ItemTemplateInput Input { get; init; } = new();
}
