using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Projects;

public static class ProjectErrors
{
    public static readonly Error NotFound =
        Error.NotFound(
            FeatureErrorCode.ProjectNotFound,
            "Project was not found.");

    public static readonly Error HasReservations =
        Error.Conflict(
            FeatureErrorCode.ProjectHasReservations,
            "Project cannot be deleted because it has reservations.");
}
