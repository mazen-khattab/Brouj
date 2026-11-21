using FluentValidation;

namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitPaymentInstallmentInputValidator : AbstractValidator<UnitPaymentInstallmentInput>
{
    public UnitPaymentInstallmentInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.InstallmentName).NotEmpty()
            .Must(name => name.Trim().Length <= 100)
            .WithMessage("Installment name must be at most 100 characters after trimming.");
        RuleFor(input => input.InstallmentNumber).GreaterThan(0);
        RuleFor(input => input.InstallmentPercentage).GreaterThan(0).LessThanOrEqualTo(100)
            .PrecisionScale(7, 4, ignoreTrailingZeros: true)
            .WithMessage("Installment percentage may have at most four decimal places.");
        RuleFor(input => input.InstallmentPeriodMonths).NotNull().GreaterThanOrEqualTo(0);
    }
}
