using FluentValidation;

namespace Brouj.Application.Features.Areas.Queries.GetLookup;

/// <summary>
/// Validator for <see cref="GetAreasLookupQuery"/>.
/// </summary>
public sealed class GetAreasLookupQueryValidator : AbstractValidator<GetAreasLookupQuery>
{
    public GetAreasLookupQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Search)
            .MaximumLength(100);
    }
}
