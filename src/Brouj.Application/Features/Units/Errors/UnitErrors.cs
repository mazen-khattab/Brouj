using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Units.Errors;

public static class UnitErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.UnitNotFound, "Unit was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.UnitConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.UnitInvalidInput, message);
}
