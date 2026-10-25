using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStageItem;

public sealed class DeletePlanStageItemCommandValidator : AbstractValidator<DeletePlanStageItemCommand>
{
    public DeletePlanStageItemCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.StageId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
