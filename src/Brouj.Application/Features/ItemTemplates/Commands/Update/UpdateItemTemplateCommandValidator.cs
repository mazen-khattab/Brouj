using Brouj.Application.Features.ItemTemplates.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.ItemTemplates.Commands.Update;

public sealed class UpdateItemTemplateCommandValidator : AbstractValidator<UpdateItemTemplateCommand>
{
    public UpdateItemTemplateCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new ItemTemplateInputValidator());
    }
}
