using Brouj.Application.Features.StageTemplates.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Commands.Update;

public sealed class UpdateStageTemplateCommandValidator : AbstractValidator<UpdateStageTemplateCommand>
{
    public UpdateStageTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new StageTemplateInputValidator());
    }
}
