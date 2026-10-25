using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Features.Customers.DTOs;

public sealed class RegisterCustomerInputValidator : AbstractValidator<RegisterCustomerInput>
{
    public RegisterCustomerInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.FName).NotEmpty().MaximumLength(100);
        RuleFor(input => input.LName).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Phone).EgyptianPhone();
        RuleFor(input => input.IdentityNumber).NationalId();
        RuleFor(input => input.Email).Email();
        RuleFor(input => input.Password).NotNull().MinimumLength(8);
    }
}
