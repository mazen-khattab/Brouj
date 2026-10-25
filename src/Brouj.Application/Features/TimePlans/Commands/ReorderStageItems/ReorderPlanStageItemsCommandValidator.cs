using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.ReorderStageItems;

public sealed class ReorderPlanStageItemsCommandValidator : AbstractValidator<ReorderPlanStageItemsCommand>
{
    public ReorderPlanStageItemsCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.StageId).NotEmpty();
        RuleFor(request => request.Items).NotNull().Must(items => items.Count <= 1000);
        RuleForEach(request => request.Items).NotNull().SetValidator(new ReorderItemInputValidator());
        RuleFor(request => request.Items).Must(items =>
            items.Select(item => item.Id).Distinct().Count() == items.Count &&
            items.Select(item => item.SortOrder).Order().SequenceEqual(Enumerable.Range(1, items.Count)))
            .WithMessage("Use unique sibling IDs and contiguous one-based positions.");
    }
}
