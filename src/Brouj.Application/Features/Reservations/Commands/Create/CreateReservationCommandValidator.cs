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

        RuleFor(request => request.Phone)
            .NotEmpty()
            .EgyptianPhone();

        When(request => !string.IsNullOrEmpty(request.IdentityNumber), () =>
        {
            RuleFor(request => request.IdentityNumber!)
                .NationalId();
        });

        When(request => !string.IsNullOrEmpty(request.AgeGroup), () =>
        {
            RuleFor(request => request.AgeGroup!)
                .MaximumLength(16);
        });

        When(request => !string.IsNullOrEmpty(request.PlaceOfResidence), () =>
        {
            RuleFor(request => request.PlaceOfResidence!)
                .MaximumLength(100);
        });

        When(request => request.PreferredFinancialBracket.HasValue, () =>
        {
            RuleFor(request => request.PreferredFinancialBracket!.Value)
                .GreaterThan(0);
        });

        When(request => request.PreferredRepaymentPeriod.HasValue, () =>
        {
            RuleFor(request => request.PreferredRepaymentPeriod!.Value)
                .GreaterThan(0);
        });
    }
}
