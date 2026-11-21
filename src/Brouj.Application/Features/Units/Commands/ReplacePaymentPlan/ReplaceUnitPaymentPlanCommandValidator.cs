using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.ReplacePaymentPlan;

public sealed class ReplaceUnitPaymentPlanCommandValidator : AbstractValidator<ReplaceUnitPaymentPlanCommand>
{
    public ReplaceUnitPaymentPlanCommandValidator()
    {
        RuleFor(request => request.UnitId).NotEmpty();
        RuleFor(request => request.Input).NotNull();
    }
}
