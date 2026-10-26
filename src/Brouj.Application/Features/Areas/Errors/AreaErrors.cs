using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Areas.Errors;

public static class AreaErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.AreaNotFound, "Area was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.AreaConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.AreaInvalidInput, message);
}
