using FluentValidation;

namespace Brouj.Application.Features.Terms.DTOs;

public sealed class CreateTermsVersionInputValidator : AbstractValidator<CreateTermsVersionInput>
{
    public CreateTermsVersionInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Title).NotEmpty().MaximumLength(300);
        RuleFor(input => input.Content).NotEmpty().MaximumLength(100000);
        RuleFor(input => input.VersionNumber).GreaterThan(0);
        RuleFor(input => input.EffectiveDate).NotEmpty();
    }
}
