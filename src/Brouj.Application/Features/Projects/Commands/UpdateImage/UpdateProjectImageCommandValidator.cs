using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.UpdateImage;

public sealed class UpdateProjectImageCommandValidator : AbstractValidator<UpdateProjectImageCommand>
{
    public UpdateProjectImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.AltText).MaximumLength(100);
    }
}
