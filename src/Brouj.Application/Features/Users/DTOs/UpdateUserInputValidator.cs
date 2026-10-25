using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Features.Users.DTOs;

public sealed class UpdateUserInputValidator : AbstractValidator<UpdateUserInput>
{
    public UpdateUserInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Email).Email();
        RuleFor(input => input.Phone).EgyptianPhone();
        RuleFor(input => input.Role).IsInEnum();
    }
}
