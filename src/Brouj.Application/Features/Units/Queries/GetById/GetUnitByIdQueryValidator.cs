using FluentValidation;

namespace Brouj.Application.Features.Units.Queries.GetById;

public sealed class GetUnitByIdQueryValidator : AbstractValidator<GetUnitByIdQuery>
{
    public GetUnitByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
