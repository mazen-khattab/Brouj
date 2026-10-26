using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.ItemTemplates.DTOs;
using MediatR;

namespace Brouj.Application.Features.ItemTemplates.Queries.GetById;

public sealed class GetItemTemplateByIdQuery : IRequest<Result<ItemTemplateDto>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid Id { get; init; }
}
