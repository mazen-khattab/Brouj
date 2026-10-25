using Brouj.Application.Common.Errors;

namespace Brouj.Application.Features.Orders.Errors;

public static class OrderErrors
{
    public static readonly Error NotFound =
        Error.NotFound(FeatureErrorCode.OrderNotFound, "Order was not found.");

    public static Error Conflict(string message) => Error.Conflict(FeatureErrorCode.OrderConflict, message);

    public static Error Validation(string message) => Error.Validation(FeatureErrorCode.OrderInvalidInput, message);
}
