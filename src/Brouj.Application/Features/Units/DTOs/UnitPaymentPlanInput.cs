namespace Brouj.Application.Features.Units.DTOs;

public sealed class UnitPaymentPlanInput
{
    public IReadOnlyList<UnitPaymentInstallmentInput>? PaymentPlan { get; init; }
}
