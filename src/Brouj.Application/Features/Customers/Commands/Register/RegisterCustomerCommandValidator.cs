using Brouj.Application.Features.Customers.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.Register;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new RegisterCustomerInputValidator());
    }
}
