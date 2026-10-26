using FluentValidation;

namespace Brouj.Application.Features.Areas.Queries.GetAll;

public sealed class GetAreasQueryValidator : AbstractValidator<GetAreasQuery>
{
    public GetAreasQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
