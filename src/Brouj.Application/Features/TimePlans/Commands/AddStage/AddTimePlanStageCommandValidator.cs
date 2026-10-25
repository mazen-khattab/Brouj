using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.AddStage;

public sealed class AddTimePlanStageCommandValidator : AbstractValidator<AddTimePlanStageCommand>
{
    public AddTimePlanStageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.TimePlanId).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new StageDraftInputValidator());
    }
}
