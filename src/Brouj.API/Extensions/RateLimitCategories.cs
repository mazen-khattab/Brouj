namespace Brouj.API.Extensions;

public static class RateLimitCategories
{
    public const string PublicReads = "PublicReads";
    public const string Authentication = "Authentication";
    public const string ReservationWrites = "ReservationWrites";
    public const string AdminWrites = "AdminWrites";
    public const string FileUploads = "FileUploads";
}
