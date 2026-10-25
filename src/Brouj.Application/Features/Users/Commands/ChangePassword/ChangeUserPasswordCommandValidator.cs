using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.ChangePassword;

public sealed class ChangeUserPasswordCommandValidator : AbstractValidator<ChangeUserPasswordCommand>
{
    public ChangeUserPasswordCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new PasswordChangeInputValidator());
    }
}
