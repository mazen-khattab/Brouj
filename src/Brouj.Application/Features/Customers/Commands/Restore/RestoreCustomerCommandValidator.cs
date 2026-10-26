using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.Restore;

public sealed class RestoreCustomerCommandValidator : AbstractValidator<RestoreCustomerCommand>
{
    public RestoreCustomerCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
