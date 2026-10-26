using Brouj.Application.Features.Users.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.Update;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new UpdateUserInputValidator());
    }
}
