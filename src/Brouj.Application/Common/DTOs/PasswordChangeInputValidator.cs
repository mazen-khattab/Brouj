using FluentValidation;

namespace Brouj.Application.Common.DTOs;

public sealed class PasswordChangeInputValidator : AbstractValidator<PasswordChangeInput>
{
    public PasswordChangeInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.CurrentPassword).NotNull().MinimumLength(1);
        RuleFor(input => input.NewPassword).NotNull().MinimumLength(8);
    }
}
