using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Commands.SetActive;

public sealed class SetStageTemplateActiveCommandValidator : AbstractValidator<SetStageTemplateActiveCommand>
{
    public SetStageTemplateActiveCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
