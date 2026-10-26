using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.Delete;

public sealed class DeleteTimePlanCommandValidator : AbstractValidator<DeleteTimePlanCommand>
{
    public DeleteTimePlanCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
