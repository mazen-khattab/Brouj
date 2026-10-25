using Brouj.Domain.Enums;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.ChangeStageItemStatus;

public sealed class ChangePlanStageItemStatusCommandValidator : AbstractValidator<ChangePlanStageItemStatusCommand>
{
    public ChangePlanStageItemStatusCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.StageId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Status).IsInEnum();
        RuleFor(request => request.ConfirmedDate).NotNull().When(request => request.Status == PlanStageItemStatus.Completed).WithMessage("Pls insert the dates first");
    }
}
