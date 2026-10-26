using FluentValidation;

namespace Brouj.Application.Features.Terms.Queries.GetActive;

public sealed class GetActiveTermsAndConditionsQueryValidator : AbstractValidator<GetActiveTermsAndConditionsQuery>
{
    public GetActiveTermsAndConditionsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
