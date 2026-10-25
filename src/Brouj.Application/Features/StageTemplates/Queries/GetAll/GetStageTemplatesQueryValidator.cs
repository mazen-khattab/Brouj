using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Queries.GetAll;

public sealed class GetStageTemplatesQueryValidator : AbstractValidator<GetStageTemplatesQuery>
{
    public GetStageTemplatesQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
