using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Users.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.UserNotFound, "User was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.UserConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.UserInvalidInput, message);
}
