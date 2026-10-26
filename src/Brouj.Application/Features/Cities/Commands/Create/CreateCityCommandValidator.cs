using Brouj.Application.Features.Cities.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Cities.Commands.Create;

public sealed class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
{
    public CreateCityCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().SetValidator(new CityInputValidator());
    }
}
