using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.Login;

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new LoginInputValidator());
    }
}
