using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Terms.DTOs;
using MediatR;

namespace Brouj.Application.Features.Terms.Commands.CreateVersion;

[InvalidatesCache("terms")]
public sealed class CreateTermsAndConditionsVersionCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public CreateTermsVersionInput Input { get; init; } = new();
}
