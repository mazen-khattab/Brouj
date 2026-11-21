using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Attributes;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using MediatR;

namespace Brouj.Application.Features.Units.Commands.ReplacePaymentPlan;

[InvalidatesCache("catalogue")]
public sealed class ReplaceUnitPaymentPlanCommand : IRequest<Result<Guid>>, IRequireAccess, ITransactionalRequest
{
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Staff;

    public Guid UnitId { get; init; }
    public UnitPaymentPlanInput Input { get; init; } = new();
}
