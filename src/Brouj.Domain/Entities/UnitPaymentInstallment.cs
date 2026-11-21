namespace Brouj.Domain.Entities;

public sealed class UnitPaymentInstallment
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string InstallmentName { get; set; } = string.Empty;
    public int InstallmentNumber { get; set; }
    public decimal InstallmentPercentage { get; set; }
    public int InstallmentPeriodMonths { get; set; }

    public BaseUnit Unit { get; set; } = null!;
}
