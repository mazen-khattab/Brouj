using FluentValidation;

namespace Brouj.Application.Features.Areas.DTOs;

public sealed class AreaInputValidator : AbstractValidator<AreaInput>
{
    public AreaInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.CityId).NotEmpty();
    }
}
