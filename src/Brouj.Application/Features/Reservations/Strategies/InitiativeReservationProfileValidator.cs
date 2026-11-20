using Brouj.Application.Common.Utils;
using Brouj.Domain.Entities;
using FluentValidation;

namespace Brouj.Application.Features.Reservations.Strategies;

// Validates only initiative profiles, not every CreateReservationCommand in the pipeline.
public sealed class InitiativeReservationProfileValidator : AbstractValidator<InitiativeReservation>
{
    public InitiativeReservationProfileValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(profile => profile.IdentityNumber).NationalId();
        RuleFor(profile => profile.AgeGroup).NotEmpty().MaximumLength(16);
        RuleFor(profile => profile.PlaceOfResidence).NotEmpty().MaximumLength(100);
        RuleFor(profile => profile.FinancialBracket).GreaterThan(0);
        RuleFor(profile => profile.RepaymentPeriod).GreaterThan(0);
    }
}
