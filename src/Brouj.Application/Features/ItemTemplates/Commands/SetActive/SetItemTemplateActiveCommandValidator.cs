using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Commands.SetActive;

public sealed class SetItemTemplateActiveCommandValidator : AbstractValidator<SetItemTemplateActiveCommand>
{
    public SetItemTemplateActiveCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
