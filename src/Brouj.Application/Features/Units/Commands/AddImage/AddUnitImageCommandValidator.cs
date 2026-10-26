using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.AddImage;

public sealed class AddUnitImageCommandValidator : AbstractValidator<AddUnitImageCommand>
{
    public AddUnitImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();
        RuleFor(request => request.File).NotNull().SetValidator(new UploadInputValidator());
        RuleFor(request => request.AltText).MaximumLength(100);
    }
}
