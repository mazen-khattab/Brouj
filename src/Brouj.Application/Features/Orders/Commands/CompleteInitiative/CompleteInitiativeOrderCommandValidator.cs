using FluentValidation;

namespace Brouj.Application.Features.Orders.Commands.CompleteInitiative;

public sealed class CompleteInitiativeOrderCommandValidator : AbstractValidator<CompleteInitiativeOrderCommand>
{
    public CompleteInitiativeOrderCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
