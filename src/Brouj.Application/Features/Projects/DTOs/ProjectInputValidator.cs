using Brouj.Domain.Enums;
using FluentValidation;

namespace Brouj.Application.Features.Projects.DTOs;

public sealed class ProjectInputValidator : AbstractValidator<ProjectInput>
{
    public ProjectInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Name).NotEmpty().MaximumLength(100);
        RuleFor(input => input.AreaId).NotEmpty();
        RuleFor(input => input.Description).MaximumLength(20000);
        RuleFor(input => input.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(input => input.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(input => input.Size).GreaterThan(0).PrecisionScale(18, 4, false);
        RuleFor(input => input.Price).GreaterThanOrEqualTo(0).PrecisionScale(19, 4, false);
        RuleFor(input => input.ProjectType).IsInEnum();
        RuleFor(input => input.FloorCount).GreaterThanOrEqualTo(0);
        RuleFor(input => input.PlannedUnitCount).GreaterThanOrEqualTo(0);
        When(input => input.ProjectType == ProjectType.Initiative, () =>
        {
            RuleFor(input => input.Price).GreaterThan(0);
            RuleFor(input => input.NeighborPrice).NotNull().GreaterThanOrEqualTo(0).PrecisionScale(19, 4, false);
            RuleFor(input => input.DrawDate).NotNull();
        });
        When(input => input.ProjectType == ProjectType.Regular, () =>
        {
            RuleFor(input => input.NeighborPrice).Null();
            RuleFor(input => input.DrawDate).Null();
        });
    }
}
