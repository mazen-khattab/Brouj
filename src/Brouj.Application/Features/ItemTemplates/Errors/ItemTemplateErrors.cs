using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.ItemTemplates.Errors;

public static class ItemTemplateErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.ItemTemplateNotFound, "ItemTemplate was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.ItemTemplateConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.ItemTemplateInvalidInput, message);
}
