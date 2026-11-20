using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Reservations.Strategies;

public sealed class InitiativeReservationPreparationStrategy(
    InitiativeReservationProfileValidator validator,
    TimeProvider clock) : IReservationPreparationStrategy
{
    public ProjectType Type => ProjectType.Initiative;

    public async Task<Result> PrepareAsync(Reservation reservation, CreateReservationCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.IdentityNumber) ||
            string.IsNullOrWhiteSpace(request.AgeGroup) ||
            string.IsNullOrWhiteSpace(request.PlaceOfResidence) ||
            !request.PreferredFinancialBracket.HasValue ||
            !request.PreferredRepaymentPeriod.HasValue)
        {
            return Result.Failure(ReservationErrors.InitiativeProfileRequired);
        }

        var profile = new InitiativeReservation
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            IdentityNumber = request.IdentityNumber.Trim(),
            AgeGroup = request.AgeGroup.Trim(),
            PlaceOfResidence = request.PlaceOfResidence.Trim(),
            FinancialBracket = request.PreferredFinancialBracket.Value,
            RepaymentPeriod = request.PreferredRepaymentPeriod.Value,
            CreatedAt = clock.GetUtcNow()
        };

        var validation = await validator.ValidateAsync(profile, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(ReservationErrors.Validation(string.Join(" ", validation.Errors.Select(error => error.ErrorMessage))));
        }

        reservation.InitiativeReservation = profile;
        return Result.Success();
    }
}
