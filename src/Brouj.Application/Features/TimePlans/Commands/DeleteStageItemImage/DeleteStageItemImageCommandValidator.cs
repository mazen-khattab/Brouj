using FluentValidation;

namespace Brouj.Application.Features.TimePlans.Commands.DeleteStageItemImage;

public sealed class DeleteStageItemImageCommandValidator : AbstractValidator<DeleteStageItemImageCommand>
{
    public DeleteStageItemImageCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ItemId).NotEmpty();
        RuleFor(request => request.Id).NotEmpty();
    }
}
