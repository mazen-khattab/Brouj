using FluentValidation;

namespace Brouj.Application.Features.Customers.Queries.GetCurrent;

public sealed class GetCurrentCustomerQueryValidator : AbstractValidator<GetCurrentCustomerQuery>
{
    public GetCurrentCustomerQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
