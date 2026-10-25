using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.Update;

public sealed class UpdateTimePlanCommandValidator : AbstractValidator<UpdateTimePlanCommand>
{
    public UpdateTimePlanCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new TimePlanInputValidator());
    }
}
