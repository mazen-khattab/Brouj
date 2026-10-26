using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Queries.GetById;

public sealed class GetItemTemplateByIdQueryValidator : AbstractValidator<GetItemTemplateByIdQuery>
{
    public GetItemTemplateByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
