using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Queries.GetAll;

public sealed class GetItemTemplatesQueryValidator : AbstractValidator<GetItemTemplatesQuery>
{
    public GetItemTemplatesQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
