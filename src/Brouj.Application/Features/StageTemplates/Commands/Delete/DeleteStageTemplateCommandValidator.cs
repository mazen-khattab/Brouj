using FluentValidation;

namespace Brouj.Application.Features.StageTemplates.Commands.Delete;

public sealed class DeleteStageTemplateCommandValidator : AbstractValidator<DeleteStageTemplateCommand>
{
    public DeleteStageTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
