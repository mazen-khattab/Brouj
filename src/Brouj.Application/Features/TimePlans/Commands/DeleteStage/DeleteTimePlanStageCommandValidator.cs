using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStage;

public sealed class DeleteTimePlanStageCommandValidator : AbstractValidator<DeleteTimePlanStageCommand>
{
    public DeleteTimePlanStageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.TimePlanId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
