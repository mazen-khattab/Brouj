using FluentValidation;

namespace Brouj.Application.Features.TimePlans.DTOs;

public sealed class StageItemDraftInputValidator : AbstractValidator<StageItemDraftInput>
{
    public StageItemDraftInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100).When(input => input.ItemTemplateId is null);
        RuleFor(input => input.DueDate).NotNull().WithMessage("Pls insert the dates first");
        RuleFor(input => input.ItemTemplateId).NotEmpty().When(input => input.ItemTemplateId is not null);
    }
}
