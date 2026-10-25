using Brouj.Application.Features.Units.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.Create;

public sealed class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new UnitInputValidator());
    }
}
