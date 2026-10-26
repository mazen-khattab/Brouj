using FluentValidation;

namespace Brouj.Application.Features.Units.Queries.GetByProject;

public sealed class GetUnitsByProjectQueryValidator : AbstractValidator<GetUnitsByProjectQuery>
{
    public GetUnitsByProjectQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
    }
}
