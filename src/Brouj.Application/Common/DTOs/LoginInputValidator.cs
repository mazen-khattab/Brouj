using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Common.DTOs;

public sealed class LoginInputValidator : AbstractValidator<LoginInput>
{
    public LoginInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Email).Email();
        RuleFor(input => input.Password).NotNull().MinimumLength(1);
    }
}
