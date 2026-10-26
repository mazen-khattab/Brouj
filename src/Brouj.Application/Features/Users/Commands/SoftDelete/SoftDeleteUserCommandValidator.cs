using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.SoftDelete;

public sealed class SoftDeleteUserCommandValidator : AbstractValidator<SoftDeleteUserCommand>
{
    public SoftDeleteUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
