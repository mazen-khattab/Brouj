using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Queries.GetById;

public sealed class GetStageTemplateByIdQueryValidator : AbstractValidator<GetStageTemplateByIdQuery>
{
    public GetStageTemplateByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
