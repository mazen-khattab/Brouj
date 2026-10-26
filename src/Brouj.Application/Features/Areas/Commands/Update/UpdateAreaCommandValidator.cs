using Brouj.Application.Features.Areas.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Areas.Commands.Update;

public sealed class UpdateAreaCommandValidator : AbstractValidator<UpdateAreaCommand>
{
    public UpdateAreaCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new AreaInputValidator());
    }
}
