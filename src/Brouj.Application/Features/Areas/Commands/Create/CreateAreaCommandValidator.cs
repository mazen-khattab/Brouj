using Brouj.Application.Features.Areas.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Areas.Commands.Create;

public sealed class CreateAreaCommandValidator : AbstractValidator<CreateAreaCommand>
{
    public CreateAreaCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new AreaInputValidator());
    }
}
