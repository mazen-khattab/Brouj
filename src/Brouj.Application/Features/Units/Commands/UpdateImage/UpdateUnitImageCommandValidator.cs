using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.UpdateImage;

public sealed class UpdateUnitImageCommandValidator : AbstractValidator<UpdateUnitImageCommand>
{
    public UpdateUnitImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.AltText).MaximumLength(100);
    }
}
