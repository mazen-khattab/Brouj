using FluentValidation;

namespace Brouj.Application.Features.TimePlans.DTOs;

public sealed class PlanStageItemInputValidator : AbstractValidator<PlanStageItemInput>
{
    public PlanStageItemInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.DueDate).NotNull().WithMessage("Pls insert the dates first");
    }
}
