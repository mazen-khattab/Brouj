namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitPaymentInstallmentDto
{
    public string InstallmentName { get; init; } = string.Empty;
    public int InstallmentNumber { get; init; }
    public decimal InstallmentPercentage { get; init; }
    public int InstallmentPeriodMonths { get; init; }
}
