using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.ReplacePaymentPlan;

public sealed class ReplaceUnitPaymentPlanCommandHandler(IApplicationDbContext db)
    : IRequestHandler<ReplaceUnitPaymentPlanCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ReplaceUnitPaymentPlanCommand request, CancellationToken cancellationToken)
    {
        await db.LockUnitAsync(request.UnitId, cancellationToken);

        var projectType = await db.BaseUnits
            .AsNoTracking()
            .Where(unit => unit.Id == request.UnitId)
            .Join(db.BaseProjects, unit => unit.ProjectId, project => project.Id, (_, project) => (ProjectType?)project.ProjectType)
            .SingleOrDefaultAsync(cancellationToken);

        if (projectType is null)
        {
            return Result<Guid>.Failure(UnitErrors.NotFound);
        }

        if (projectType != ProjectType.Regular)
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Payment plans apply only to regular units."));
        }

        if (UnitPaymentPlanInputValidator.GetError(request.Input) is { } error)
        {
            return Result<Guid>.Failure(error);
        }

        var previous = await db.UnitPaymentInstallments.Where(installment => installment.UnitId == request.UnitId)
            .ToDictionaryAsync(installment => installment.InstallmentNumber, cancellationToken);

        // Keep the row for each retained number so no insert competes with an existing unique key.
        foreach (var input in request.Input.PaymentPlan!)
        {
            if (!previous.Remove(input.InstallmentNumber, out var installment))
            {
                installment = new UnitPaymentInstallment
                {
                    Id = Guid.NewGuid(),
                    UnitId = request.UnitId,
                    InstallmentNumber = input.InstallmentNumber
                };
                db.UnitPaymentInstallments.Add(installment);
            }

            installment.InstallmentName = input.InstallmentName.Trim();
            installment.InstallmentPercentage = input.InstallmentPercentage;
            installment.InstallmentPeriodMonths = input.InstallmentPeriodMonths!.Value;
        }

        db.UnitPaymentInstallments.RemoveRange(previous.Values);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.UnitId);
    }
}
