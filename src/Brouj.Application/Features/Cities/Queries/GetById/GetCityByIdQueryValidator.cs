using FluentValidation;

namespace Brouj.Application.Features.Cities.Queries.GetById;

public sealed class GetCityByIdQueryValidator : AbstractValidator<GetCityByIdQuery>
{
    public GetCityByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
