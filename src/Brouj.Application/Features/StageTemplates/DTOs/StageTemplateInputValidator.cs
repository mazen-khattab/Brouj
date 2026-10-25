using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.DTOs;

public sealed class StageTemplateInputValidator : AbstractValidator<StageTemplateInput>
{
    public StageTemplateInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Code).NotEmpty().MaximumLength(64);
    }
}
