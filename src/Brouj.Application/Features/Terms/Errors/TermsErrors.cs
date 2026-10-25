using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Terms.Errors;

public static class TermsErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.TermsNotFound, "Terms was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.TermsConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.TermsInvalidInput, message);
}
