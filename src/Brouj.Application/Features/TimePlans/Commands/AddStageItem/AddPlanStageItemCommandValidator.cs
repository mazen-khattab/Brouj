using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageItem;

public sealed class AddPlanStageItemCommandValidator : AbstractValidator<AddPlanStageItemCommand>
{
    public AddPlanStageItemCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.StageId).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new PlanStageItemInputValidator());
    }
}
