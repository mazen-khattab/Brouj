using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class OrderStatusExtensions
{
    public static string ToDatabaseString(this OrderStatus value) => value switch
    {
        OrderStatus.Pending => "pending",
        OrderStatus.Processing => "processing",
        OrderStatus.Completed => "completed",
        OrderStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown order status.")
    };

    public static OrderStatus ToOrderStatus(this string value) => value switch
    {
        "pending" => OrderStatus.Pending,
        "processing" => OrderStatus.Processing,
        "completed" => OrderStatus.Completed,
        "cancelled" => OrderStatus.Cancelled,
        _ => throw new ArgumentException("Unknown database value for order status.", nameof(value))
    };
}
