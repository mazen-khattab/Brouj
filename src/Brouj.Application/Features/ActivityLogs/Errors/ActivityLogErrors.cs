using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.ActivityLogs.Errors;

public static class ActivityLogErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.ActivityLogNotFound, "ActivityLog was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.ActivityLogConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.ActivityLogInvalidInput, message);
}
