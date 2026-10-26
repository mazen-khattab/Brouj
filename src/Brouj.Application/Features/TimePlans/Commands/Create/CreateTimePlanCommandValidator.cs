using Brouj.Application.Features.TimePlans.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.Create;

public sealed class CreateTimePlanCommandValidator : AbstractValidator<CreateTimePlanCommand>
{
    public CreateTimePlanCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new TimePlanInputValidator());
        RuleFor(request => request.Stages).NotNull().Must(stages => stages.Count <= 100);
        RuleForEach(request => request.Stages).NotNull().SetValidator(new StageDraftInputValidator());
    }
}
