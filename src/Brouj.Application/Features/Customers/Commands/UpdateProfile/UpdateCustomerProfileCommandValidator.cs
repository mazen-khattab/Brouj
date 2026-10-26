using Brouj.Application.Features.Customers.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.UpdateProfile;

public sealed class UpdateCustomerProfileCommandValidator : AbstractValidator<UpdateCustomerProfileCommand>
{
    public UpdateCustomerProfileCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new CustomerProfileInputValidator());
    }
}
