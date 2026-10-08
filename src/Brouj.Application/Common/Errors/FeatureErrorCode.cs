namespace Brouj.Application.Common.Errors;

public enum FeatureErrorCode
{
    None = 0,

    // Authentication
    InvalidCredentials,

    // Projects
    ProjectNotFound,
    ProjectHasReservations,

    // Units
    UnitNotFound,

    // Reservations
    ReservationNotFound,
    ReservationAlreadyActive,
    ReservationAlreadyCancelled,

    // Orders
    OrderNotFound,
    OrderAlreadyCompleted
}
