namespace Brouj.Application.Features.Units.DTOs;

public sealed class CreateUnitInput : UnitInput
{
    public IReadOnlyList<UnitPaymentInstallmentInput>? PaymentPlan { get; init; }
}
