using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStageItem;

public sealed class UpdatePlanStageItemCommandValidator : AbstractValidator<UpdatePlanStageItemCommand>
{
    public UpdatePlanStageItemCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.StageId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new PlanStageItemInputValidator());
    }
}
