using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Areas.Commands.ReplaceAmenities;

public sealed class ReplaceAreaAmenitiesCommandValidator : AbstractValidator<ReplaceAreaAmenitiesCommand>
{
    public ReplaceAreaAmenitiesCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.AreaId).NotEmpty();
        RuleFor(request => request.Amenities).NotNull().Must(items => items.Count <= 100);
        RuleForEach(request => request.Amenities).NotNull().SetValidator(new AmenityDtoValidator());
    }
}
