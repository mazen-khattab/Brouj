using FluentValidation;

namespace Brouj.Application.Features.Projects.Queries.GetLookup;

/// <summary>
/// Validator for <see cref="GetProjectsLookupQuery"/>.
/// </summary>
public sealed class GetProjectsLookupQueryValidator : AbstractValidator<GetProjectsLookupQuery>
{
    public GetProjectsLookupQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Search)
            .MaximumLength(100);

        RuleFor(request => request.Type)
            .IsInEnum()
            .When(request => request.Type is not null);
    }
}
