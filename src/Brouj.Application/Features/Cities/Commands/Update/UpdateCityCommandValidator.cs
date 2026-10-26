using Brouj.Application.Features.Cities.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Cities.Commands.Update;

public sealed class UpdateCityCommandValidator : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
        RuleFor(request => request.Input).NotNull().SetValidator(new CityInputValidator());
    }
}
