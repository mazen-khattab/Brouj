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
    OrderAlreadyCompleted,

    // Part 7 codes are appended to preserve the numeric values of existing contracts.
    AccessDenied,
    CustomerNotFound,
    CustomerConflict,
    CustomerInvalidInput,
    UserNotFound,
    UserConflict,
    UserInvalidInput,
    CityNotFound,
    CityConflict,
    CityInvalidInput,
    AreaNotFound,
    AreaConflict,
    AreaInvalidInput,
    ProjectConflict,
    ProjectInvalidInput,
    UnitConflict,
    UnitInvalidInput,
    ReservationConflict,
    ReservationInvalidInput,
    OrderConflict,
    OrderInvalidInput,
    TimePlanNotFound,
    TimePlanConflict,
    TimePlanInvalidInput,
    StageTemplateNotFound,
    StageTemplateConflict,
    StageTemplateInvalidInput,
    ItemTemplateNotFound,
    ItemTemplateConflict,
    ItemTemplateInvalidInput,
    TermsNotFound,
    TermsConflict,
    TermsInvalidInput,
    ActivityLogNotFound,
    ActivityLogConflict,
    ActivityLogInvalidInput
}
