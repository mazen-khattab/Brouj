using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Customers.Errors;

public static class CustomerErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.CustomerNotFound, "Customer was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.CustomerConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.CustomerInvalidInput, message);
}
