using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitInputValidator : AbstractValidator<UnitInput>
{
    public UnitInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input).Must(input => Pricing.IsValidTotal(input.Size, input.MeterPrice))
            .WithMessage("The calculated total exceeds the supported amount.");

        RuleFor(input => input.Number).NotEmpty().MaximumLength(64);
        RuleFor(input => input.MeterPrice).GreaterThanOrEqualTo(0).PrecisionScale(19, 4, false);
        RuleFor(input => input.Size).GreaterThan(0).PrecisionScale(18, 4, false);
        RuleFor(input => input.FloorNumber).GreaterThanOrEqualTo(0);
        RuleFor(input => input.NeighborMeterPrice).GreaterThanOrEqualTo(0).PrecisionScale(19, 4, false);
    }
}
