using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Commands.Delete;

public sealed class DeleteItemTemplateCommandValidator : AbstractValidator<DeleteItemTemplateCommand>
{
    public DeleteItemTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
