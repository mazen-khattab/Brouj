using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Features.Reservations.Commands.Create;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();

        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.IdentityNumber)
            .NationalId();

        RuleFor(request => request.Phone)
            .EgyptianPhone();

        RuleFor(request => request.AgeGroup)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(request => request.PlaceOfResidence)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.PreferredFinancialBracket)
            .GreaterThan(0);

        RuleFor(request => request.PreferredRepaymentPeriod)
            .GreaterThan(0);
    }
}
