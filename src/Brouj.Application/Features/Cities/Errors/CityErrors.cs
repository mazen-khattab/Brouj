using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Cities.Errors;

public static class CityErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.CityNotFound, "City was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.CityConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.CityInvalidInput, message);
}
