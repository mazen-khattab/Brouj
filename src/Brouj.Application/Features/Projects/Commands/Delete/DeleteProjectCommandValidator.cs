using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.Delete;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
