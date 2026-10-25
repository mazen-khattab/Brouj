namespace Brouj.Application.Common.Utils;

public static class Pricing
{
    public static bool IsValidTotal(decimal size, decimal meterPrice)
    {
        try
        {
            // The order snapshot must fit the approved decimal(19,4) database column.
            return Total(size, meterPrice) <= 999999999999999.99m;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    // The approved garage price is zero and is included in every displayed/payable total.
    public static decimal Total(decimal size, decimal meterPrice) => decimal.Ceiling(size * meterPrice * 100m) / 100m;

    public static decimal Percentage(decimal total, decimal projectPrice) =>
        decimal.Round(total / projectPrice * 100m, 6, MidpointRounding.AwayFromZero);
}
