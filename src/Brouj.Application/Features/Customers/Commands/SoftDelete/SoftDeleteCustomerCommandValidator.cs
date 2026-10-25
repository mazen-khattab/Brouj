using FluentValidation;

namespace Brouj.Application.Features.Customers.Commands.SoftDelete;

public sealed class SoftDeleteCustomerCommandValidator : AbstractValidator<SoftDeleteCustomerCommand>
{
    public SoftDeleteCustomerCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
