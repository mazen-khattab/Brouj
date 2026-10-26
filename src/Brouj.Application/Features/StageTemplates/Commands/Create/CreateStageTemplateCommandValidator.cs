using Brouj.Application.Features.StageTemplates.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Commands.Create;

public sealed class CreateStageTemplateCommandValidator : AbstractValidator<CreateStageTemplateCommand>
{
    public CreateStageTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new StageTemplateInputValidator());
    }
}
