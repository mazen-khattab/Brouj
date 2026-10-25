using FluentValidation;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetAll;

public sealed class GetActivityLogsQueryValidator : AbstractValidator<GetActivityLogsQuery>
{
    public GetActivityLogsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.UserId).NotEmpty().When(request => request.UserId is not null);
        RuleFor(request => request.EntityType).MaximumLength(128);
        RuleFor(request => request.Action).IsInEnum().When(request => request.Action is not null);
        RuleFor(request => request.DateTo).GreaterThanOrEqualTo(request => request.DateFrom)
            .When(request => request.DateFrom is not null && request.DateTo is not null);
    }
}
