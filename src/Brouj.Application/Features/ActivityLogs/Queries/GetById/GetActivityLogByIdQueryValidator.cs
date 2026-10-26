using FluentValidation;

namespace Brouj.Application.Features.ActivityLogs.Queries.GetById;

public sealed class GetActivityLogByIdQueryValidator : AbstractValidator<GetActivityLogByIdQuery>
{
    public GetActivityLogByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
