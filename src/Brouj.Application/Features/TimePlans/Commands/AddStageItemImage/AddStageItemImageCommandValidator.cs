using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.AddStageItemImage;

public sealed class AddStageItemImageCommandValidator : AbstractValidator<AddStageItemImageCommand>
{
    public AddStageItemImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ItemId).NotEmpty();
        RuleFor(request => request.File).NotNull().SetValidator(new UploadInputValidator());
        RuleFor(request => request.Description).MaximumLength(20000);
    }
}
