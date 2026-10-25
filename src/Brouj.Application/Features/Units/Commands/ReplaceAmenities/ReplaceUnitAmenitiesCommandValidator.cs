using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Units.Commands.ReplaceAmenities;

public sealed class ReplaceUnitAmenitiesCommandValidator : AbstractValidator<ReplaceUnitAmenitiesCommand>
{
    public ReplaceUnitAmenitiesCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.UnitId).NotEmpty();
        RuleFor(request => request.Amenities).NotNull().Must(items => items.Count <= 100);
        RuleForEach(request => request.Amenities).NotNull().SetValidator(new AmenityDtoValidator());
    }
}
