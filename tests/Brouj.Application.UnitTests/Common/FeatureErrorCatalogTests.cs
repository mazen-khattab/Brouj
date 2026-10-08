using Brouj.Application.Common.Errors;
using Brouj.Application.Features.Projects;
using Brouj.Application.Features.Reservations;

namespace Brouj.Application.UnitTests.Common;

public sealed class FeatureErrorCatalogTests
{
    public static TheoryData<Error, ErrorType, FeatureErrorCode> CatalogErrors => new()
    {
        {
            ProjectErrors.NotFound,
            ErrorType.NotFound,
            FeatureErrorCode.ProjectNotFound
        },
        {
            ProjectErrors.HasReservations,
            ErrorType.Conflict,
            FeatureErrorCode.ProjectHasReservations
        },
        {
            ReservationErrors.NotFound,
            ErrorType.NotFound,
            FeatureErrorCode.ReservationNotFound
        },
        {
            ReservationErrors.AlreadyActive,
            ErrorType.Conflict,
            FeatureErrorCode.ReservationAlreadyActive
        },
        {
            ReservationErrors.AlreadyCancelled,
            ErrorType.Conflict,
            FeatureErrorCode.ReservationAlreadyCancelled
        }
    };

    [Theory]
    [MemberData(nameof(CatalogErrors))]
    public void CatalogError_ExposesExpectedTypeAndCode(
        Error error,
        ErrorType expectedType,
        FeatureErrorCode expectedCode)
    {
        Assert.Equal(expectedType, error.Type);
        Assert.Equal(expectedCode, error.Code);
    }
}
