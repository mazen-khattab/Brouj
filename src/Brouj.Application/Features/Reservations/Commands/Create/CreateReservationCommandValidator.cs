using FluentValidation;

namespace Brouj.Application.Features.Reservations.Commands.Create;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();
    }
}
