using System.Text.Json.Serialization;

namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitPaymentInstallmentInput
{
    public string InstallmentName { get; init; } = string.Empty;

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int InstallmentNumber { get; init; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public decimal InstallmentPercentage { get; init; }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int? InstallmentPeriodMonths { get; init; }
}
