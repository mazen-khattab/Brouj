using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.Logout;

public sealed class LogoutCustomerCommandValidator : AbstractValidator<LogoutCustomerCommand>
{
    public LogoutCustomerCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
