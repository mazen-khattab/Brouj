using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Projects.Errors;

public static class ProjectErrors
{
    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.ProjectConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.ProjectInvalidInput, message);

    public static readonly Error NotFound =
        Error.NotFound(
            FeatureErrorCode.ProjectNotFound,
            "Project was not found.");

    public static readonly Error HasReservations =
        Error.Conflict(
            FeatureErrorCode.ProjectHasReservations,
            "Project cannot be deleted because it has reservations.");
}
