using FluentValidation;

namespace Brouj.Application.Features.TimePlans.DTOs;

public sealed class ReorderItemInputValidator : AbstractValidator<ReorderItemInput>
{
    public ReorderItemInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Id).NotEmpty();
        RuleFor(input => input.SortOrder).GreaterThan(0);
    }
}
