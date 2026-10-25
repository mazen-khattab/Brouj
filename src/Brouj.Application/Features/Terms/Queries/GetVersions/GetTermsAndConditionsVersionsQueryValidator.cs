using FluentValidation;

namespace Brouj.Application.Features.Terms.Queries.GetVersions;

public sealed class GetTermsAndConditionsVersionsQueryValidator : AbstractValidator<GetTermsAndConditionsVersionsQuery>
{
    public GetTermsAndConditionsVersionsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
