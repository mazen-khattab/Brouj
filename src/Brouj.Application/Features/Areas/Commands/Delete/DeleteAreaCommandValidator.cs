using FluentValidation;

namespace Brouj.Application.Features.Areas.Commands.Delete;

public sealed class DeleteAreaCommandValidator : AbstractValidator<DeleteAreaCommand>
{
    public DeleteAreaCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
