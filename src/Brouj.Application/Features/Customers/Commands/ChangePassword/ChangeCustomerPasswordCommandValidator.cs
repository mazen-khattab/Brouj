using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.ChangePassword;

public sealed class ChangeCustomerPasswordCommandValidator : AbstractValidator<ChangeCustomerPasswordCommand>
{
    public ChangeCustomerPasswordCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new PasswordChangeInputValidator());
    }
}
