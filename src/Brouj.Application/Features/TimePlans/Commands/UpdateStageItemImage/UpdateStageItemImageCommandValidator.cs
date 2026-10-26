using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.UpdateStageItemImage;

public sealed class UpdateStageItemImageCommandValidator : AbstractValidator<UpdateStageItemImageCommand>
{
    public UpdateStageItemImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ItemId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Description).MaximumLength(20000);
    }
}
