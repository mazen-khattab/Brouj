using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Queries.GetByProject;

public sealed class GetProjectTimePlanQueryValidator : AbstractValidator<GetProjectTimePlanQuery>
{
    public GetProjectTimePlanQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
    }
}
