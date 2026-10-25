using FluentValidation;

namespace Brouj.Application.Common.DTOs;

public sealed class AmenityDtoValidator : AbstractValidator<AmenityDto>
{
    public AmenityDtoValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Value).NotEmpty().MaximumLength(32);
    }
}
