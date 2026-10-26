using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.Login;

public sealed class LoginCustomerCommandValidator : AbstractValidator<LoginCustomerCommand>
{
    public LoginCustomerCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new LoginInputValidator());
    }
}
