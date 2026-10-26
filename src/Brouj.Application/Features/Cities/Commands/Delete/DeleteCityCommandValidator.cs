using FluentValidation;

namespace Brouj.Application.Features.Cities.Commands.Delete;

public sealed class DeleteCityCommandValidator : AbstractValidator<DeleteCityCommand>
{
    public DeleteCityCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
