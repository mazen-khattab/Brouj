using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.Restore;

public sealed class RestoreUserCommandValidator : AbstractValidator<RestoreUserCommand>
{
    public RestoreUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
