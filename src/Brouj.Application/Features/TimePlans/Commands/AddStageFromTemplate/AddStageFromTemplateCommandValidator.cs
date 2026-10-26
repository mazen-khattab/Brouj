using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageFromTemplate;

public sealed class AddStageFromTemplateCommandValidator : AbstractValidator<AddStageFromTemplateCommand>
{
    public AddStageFromTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.TimePlanId).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new StageDraftInputValidator());
        RuleFor(request => request.Input.StageTemplateId).NotNull().NotEmpty();
    }
}
