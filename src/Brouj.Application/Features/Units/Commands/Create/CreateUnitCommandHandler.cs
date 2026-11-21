using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Models;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Units.DTOs;
using Brouj.Application.Features.Units.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Features.Units.Commands.Create;

public sealed class CreateUnitCommandHandler : IRequestHandler<CreateUnitCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;

    public CreateUnitCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        await _db.LockProjectAsync(request.ProjectId, cancellationToken);
        var project = await _db.BaseProjects
            .AsNoTracking()
            .SingleOrDefaultAsync(project => project.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("The selected project does not exist."));
        }

        var entity = new BaseUnit { Id = Guid.NewGuid(), ProjectId = project.Id };
        var total = Pricing.Total(request.Input.Size, request.Input.MeterPrice);
        var isInitiative = project.ProjectType == ProjectType.Initiative;
        var paymentPlan = request.Input.PaymentPlan;

        if (isInitiative && paymentPlan is { Count: > 0 })
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Payment plans apply only to regular units."));
        }

        // Project-dependent rules return a feature error so the HTTP response includes the validation message.
        if (!isInitiative &&
            UnitPaymentPlanInputValidator.GetError(new UnitPaymentPlanInput { PaymentPlan = paymentPlan }) is { } error)
        {
            return Result<Guid>.Failure(error);
        }

        if (isInitiative && (project.Price <= 0 || total > project.Price || request.Input.NeighborMeterPrice is null))
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Initiative units require a display neighbor price"));
        }

        if (!isInitiative && request.Input.NeighborMeterPrice is not null)
        {
            return Result<Guid>.Failure(UnitErrors.Validation("Neighbor meter price applies only to initiative units."));
        }

        var isUnitNumberExist = await _db.BaseUnits
            .AsNoTracking()
            .AnyAsync(unit => unit.ProjectId == project.Id && unit.Id != entity.Id && unit.Number == request.Input.Number.Trim(), cancellationToken);

        if (isUnitNumberExist)
        {
            return Result<Guid>.Failure(UnitErrors.Conflict("The unit number is already used in this project."));
        }

        entity.Number = request.Input.Number.Trim();
        entity.MeterPrice = request.Input.MeterPrice;
        entity.FloorNumber = request.Input.FloorNumber;
        entity.Size = request.Input.Size;

        if (isInitiative)
        {
            var initiative = new InitiativeUnit
            {
                UnitId = entity.Id,
                Unit = entity,
                NeighborMeterPrice = request.Input.NeighborMeterPrice!.Value,
                Percentage = Pricing.Percentage(total, project.Price)
            };

            _db.InitiativeUnits.Add(initiative);
        }

        _db.BaseUnits.Add(entity);

        if (!isInitiative)
        {
            _db.UnitPaymentInstallments.AddRange(paymentPlan!.Select(input => new UnitPaymentInstallment
            {
                Id = Guid.NewGuid(),
                UnitId = entity.Id,
                InstallmentName = input.InstallmentName.Trim(),
                InstallmentNumber = input.InstallmentNumber,
                InstallmentPercentage = input.InstallmentPercentage,
                InstallmentPeriodMonths = input.InstallmentPeriodMonths!.Value
            }));
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
