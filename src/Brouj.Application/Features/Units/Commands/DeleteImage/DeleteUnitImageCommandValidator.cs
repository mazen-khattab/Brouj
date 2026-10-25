using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.DeleteImage;

public sealed class DeleteUnitImageCommandValidator : AbstractValidator<DeleteUnitImageCommand>
{
    public DeleteUnitImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
