using FluentValidation;

namespace Brouj.Application.Features.Cities.DTOs;

public sealed class CityInputValidator : AbstractValidator<CityInput>
{
    public CityInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
    }
}
