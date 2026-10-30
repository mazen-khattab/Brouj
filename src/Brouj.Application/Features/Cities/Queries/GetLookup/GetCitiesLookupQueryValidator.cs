using FluentValidation;

namespace Brouj.Application.Features.Cities.Queries.GetLookup;

/// <summary>
/// Validator for <see cref="GetCitiesLookupQuery"/>.
/// </summary>
public sealed class GetCitiesLookupQueryValidator : AbstractValidator<GetCitiesLookupQuery>
{
    public GetCitiesLookupQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Search)
            .MaximumLength(100);
    }
}
