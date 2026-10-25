using Brouj.Application.Features.Projects.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.Update;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new ProjectInputValidator());
    }
}
