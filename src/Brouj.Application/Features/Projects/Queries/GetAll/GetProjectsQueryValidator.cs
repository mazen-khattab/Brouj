using FluentValidation;

namespace Brouj.Application.Features.Projects.Queries.GetAll;

public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
        RuleFor(request => request.CityId).NotEmpty().When(request => request.CityId is not null);
        RuleFor(request => request.AreaId).NotEmpty().When(request => request.AreaId is not null);
        RuleFor(request => request.ProjectType).IsInEnum().When(request => request.ProjectType is not null);
    }
}
