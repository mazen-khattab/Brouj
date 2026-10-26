using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStage;

public sealed class UpdateTimePlanStageCommandValidator : AbstractValidator<UpdateTimePlanStageCommand>
{
    public UpdateTimePlanStageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.TimePlanId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new TimePlanInputValidator());
    }
}
