using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Reservations.Errors;

public static class ReservationErrors
{
    public static Error Validation(string message) =>
        Error.Validation(FeatureErrorCode.ReservationInvalidInput, message);

    public static Error Conflict(string message) 
        => Error.Conflict(
            FeatureErrorCode.ReservationConflict, 
            message);

    public static readonly Error NotFound =
        Error.NotFound(
            FeatureErrorCode.ReservationNotFound,
            "Reservation was not found.");

    public static readonly Error AlreadyActive =
        Error.Conflict(
            FeatureErrorCode.ReservationAlreadyActive,
            "An active reservation already exists.");

    public static readonly Error AlreadyCancelled =
        Error.Conflict(
            FeatureErrorCode.ReservationAlreadyCancelled,
            "Reservation is already cancelled.");

    public static readonly Error InitiativeProfileRequired =
        Error.Validation(
            FeatureErrorCode.ReservationInvalidInput,
            "Initiative demographic and financial profile is required when reserving an initiative unit.");
}
