using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.DeleteImage;

public sealed class DeleteProjectImageCommandValidator : AbstractValidator<DeleteProjectImageCommand>
{
    public DeleteProjectImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
