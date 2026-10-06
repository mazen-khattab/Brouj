using Brouj.Domain.Enums;

namespace Brouj.Domain.Extensions;

public static class ReservationStatusExtensions
{
    public static string ToDatabaseString(this ReservationStatus value) => value switch
    {
        ReservationStatus.Processing => "processing",
        ReservationStatus.Cancelled => "cancelled",
        ReservationStatus.Confirmed => "confirmed",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown reservation status.")
    };

    public static ReservationStatus ToReservationStatus(this string value) => value switch
    {
        "processing" => ReservationStatus.Processing,
        "cancelled" => ReservationStatus.Cancelled,
        "confirmed" => ReservationStatus.Confirmed,
        _ => throw new ArgumentException("Unknown database value for reservation status.", nameof(value))
    };
}
