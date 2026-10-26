using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.AddImage;

public sealed class AddProjectImageCommandValidator : AbstractValidator<AddProjectImageCommand>
{
    public AddProjectImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.File).NotNull().SetValidator(new UploadInputValidator());
        RuleFor(request => request.AltText).MaximumLength(100);
    }
}
