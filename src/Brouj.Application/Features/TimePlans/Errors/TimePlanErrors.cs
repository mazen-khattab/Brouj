using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.TimePlans.Errors;

public static class TimePlanErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.TimePlanNotFound, "TimePlan was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.TimePlanConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.TimePlanInvalidInput, message);
}
