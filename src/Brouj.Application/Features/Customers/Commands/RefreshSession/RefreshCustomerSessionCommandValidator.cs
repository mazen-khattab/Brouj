using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.RefreshSession;

public sealed class RefreshCustomerSessionCommandValidator : AbstractValidator<RefreshCustomerSessionCommand>
{
    public RefreshCustomerSessionCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
