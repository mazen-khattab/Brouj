using FluentValidation;

namespace Brouj.Application.Features.Terms.Commands.Activate;

public sealed class ActivateTermsAndConditionsCommandValidator : AbstractValidator<ActivateTermsAndConditionsCommand>
{
    public ActivateTermsAndConditionsCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
