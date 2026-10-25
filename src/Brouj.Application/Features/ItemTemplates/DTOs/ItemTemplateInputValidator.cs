using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.DTOs;

public sealed class ItemTemplateInputValidator : AbstractValidator<ItemTemplateInput>
{
    public ItemTemplateInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Code).NotEmpty().MaximumLength(64);
    }
}
