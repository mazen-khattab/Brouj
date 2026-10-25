using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.ItemTemplates.Commands.Update;

public sealed class UpdateItemTemplateCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }

    public ItemTemplateInput Input { get; init; } = new();
}
