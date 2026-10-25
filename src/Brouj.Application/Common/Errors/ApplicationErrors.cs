namespace Brouj.Application.Common.Errors;


public static class ApplicationErrors
{
    public static readonly Error InvalidCredentials =
        Error.Unauthorized(FeatureErrorCode.InvalidCredentials, "Invalid credentials.");

    public static readonly Error Forbidden =
        Error.Forbidden(FeatureErrorCode.AccessDenied, "Access is denied.");

    public static readonly Error Unauthenticated =
        Error.Unauthorized(FeatureErrorCode.AccessDenied, "Authentication is required.");
}
