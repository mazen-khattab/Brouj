using FluentValidation;

namespace Brouj.Application.Features.TimePlans.DTOs;

public sealed class StageDraftInputValidator : AbstractValidator<StageDraftInput>
{
    public StageDraftInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100).When(input => input.StageTemplateId is null);
        RuleFor(input => input.StageTemplateId).NotEmpty().When(input => input.StageTemplateId is not null);
        RuleFor(input => input.DeliveryDate).NotNull().WithMessage("Pls insert the dates first");
        RuleFor(input => input.Items).NotNull().Must(items => items.Count <= 100);
        RuleForEach(input => input.Items).NotNull().SetValidator(new StageItemDraftInputValidator());
    }
}
