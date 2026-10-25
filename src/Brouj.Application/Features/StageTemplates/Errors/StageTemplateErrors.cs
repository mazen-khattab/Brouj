using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.StageTemplates.Errors;

public static class StageTemplateErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.StageTemplateNotFound, "StageTemplate was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.StageTemplateConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.StageTemplateInvalidInput, message);
}
