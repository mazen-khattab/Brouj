using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Cities.DTOs;
using MediatR;

namespace Brouj.Application.Features.Cities.Commands.Create;

[InvalidatesCache("catalogue")]
public sealed class CreateCityCommand : IRequest<Result<Guid>>, IRequireAccess
{
    // Fixed permission metadata is not part of the client-writable request contract.
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public CityInput Input { get; init; } = new();
}
