using Brouj.Application.Features.StageTemplates.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Commands.SetItems;

public sealed class SetStageTemplateItemsCommandValidator : AbstractValidator<SetStageTemplateItemsCommand>
{
    public SetStageTemplateItemsCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Items).NotNull().Must(items => items.Count <= 100);
        RuleForEach(request => request.Items).NotNull().SetValidator(new StageTemplateItemInputValidator());
        RuleFor(request => request.Items).Must(items =>
            items.Select(item => item.ItemTemplateId).Distinct().Count() == items.Count &&
            items.Select(item => item.SortOrder).Order().SequenceEqual(Enumerable.Range(1, items.Count)))
            .WithMessage("Supply unique templates and contiguous one-based positions.");
    }
}
