using Brouj.Application.Features.Projects.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.Create;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new ProjectInputValidator());
    }
}
