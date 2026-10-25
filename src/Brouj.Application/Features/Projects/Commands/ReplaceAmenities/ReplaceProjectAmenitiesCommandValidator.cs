using Brouj.Application.Common.DTOs;
using FluentValidation;

namespace Brouj.Application.Features.Projects.Commands.ReplaceAmenities;

public sealed class ReplaceProjectAmenitiesCommandValidator : AbstractValidator<ReplaceProjectAmenitiesCommand>
{
    public ReplaceProjectAmenitiesCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Amenities).NotNull().Must(items => items.Count <= 100);
        RuleForEach(request => request.Amenities).NotNull().SetValidator(new AmenityDtoValidator());
    }
}
