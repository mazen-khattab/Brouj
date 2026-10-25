using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.DTOs;

public sealed class StageTemplateItemInputValidator : AbstractValidator<StageTemplateItemInput>
{
    public StageTemplateItemInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.ItemTemplateId).NotEmpty();
        RuleFor(input => input.SortOrder).GreaterThan(0);
    }
}
