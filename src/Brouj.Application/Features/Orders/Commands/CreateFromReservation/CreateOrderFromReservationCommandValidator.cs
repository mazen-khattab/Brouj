using FluentValidation;

namespace Brouj.Application.Features.Orders.Commands.CreateFromReservation;

public sealed class CreateOrderFromReservationCommandValidator : AbstractValidator<CreateOrderFromReservationCommand>
{
    public CreateOrderFromReservationCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ReservationId).NotEmpty();
    }
}
