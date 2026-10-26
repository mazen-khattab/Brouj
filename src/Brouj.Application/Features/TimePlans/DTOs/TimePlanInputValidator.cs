using FluentValidation;

namespace Brouj.Application.Features.TimePlans.DTOs;

public sealed class TimePlanInputValidator : AbstractValidator<TimePlanInput>
{
    public TimePlanInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.DeliveryDate).NotNull().WithMessage("Pls insert the dates first");
    }
}
