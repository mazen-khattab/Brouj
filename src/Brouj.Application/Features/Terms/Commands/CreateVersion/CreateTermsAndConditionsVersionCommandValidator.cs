using Brouj.Application.Features.Terms.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Terms.Commands.CreateVersion;

public sealed class CreateTermsAndConditionsVersionCommandValidator : AbstractValidator<CreateTermsAndConditionsVersionCommand>
{
    public CreateTermsAndConditionsVersionCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new CreateTermsVersionInputValidator());
    }
}
