using FluentValidation;

namespace Brouj.Application.Features.Terms.Queries.GetById;

public sealed class GetTermsAndConditionsByIdQueryValidator : AbstractValidator<GetTermsAndConditionsByIdQuery>
{
    public GetTermsAndConditionsByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
