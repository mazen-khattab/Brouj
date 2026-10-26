using Brouj.Application.Features.ItemTemplates.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Commands.Create;

public sealed class CreateItemTemplateCommandValidator : AbstractValidator<CreateItemTemplateCommand>
{
    public CreateItemTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new ItemTemplateInputValidator());
    }
}
