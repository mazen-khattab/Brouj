using Brouj.Application.Common.Errors;
using Brouj.Application.Features.Units.Errors;
using FluentValidation;

namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitPaymentPlanInputValidator : AbstractValidator<UnitPaymentPlanInput>
{
    public UnitPaymentPlanInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.PaymentPlan)
            .NotEmpty()
            .WithMessage("A regular unit requires at least one payment installment.");

        RuleForEach(input => input.PaymentPlan)
            .NotNull()
            .WithMessage("Each installment must be an object.")
            .SetValidator(new UnitPaymentInstallmentInputValidator());

        RuleFor(input => input.PaymentPlan).Custom((plan, context) =>
        {
            var numbers = new HashSet<int>();
            var total = 0m;

            foreach (var installment in plan!)
            {
                if (!numbers.Add(installment.InstallmentNumber))
                {
                    context.AddFailure(FormattableString.Invariant($"Installment number {installment.InstallmentNumber} is used more than once."));
                    return;
                }

                total += installment.InstallmentPercentage;
            }

            if (total != 100m)
            {
                context.AddFailure(FormattableString.Invariant($"Installment percentages must total 100%. The submitted total is {total:0.####}%."));
            }
        });
    }

    public static Error? GetError(UnitPaymentPlanInput input)
    {
        var failure = new UnitPaymentPlanInputValidator().Validate(input).Errors.FirstOrDefault();

        if (failure is null)
        {
            return null;
        }

        var message = failure.PropertyName.StartsWith("PaymentPlan[", StringComparison.Ordinal)
            ? $"{failure.PropertyName}: {failure.ErrorMessage}"
            : failure.ErrorMessage;

        return UnitErrors.Validation(message);
    }
}
