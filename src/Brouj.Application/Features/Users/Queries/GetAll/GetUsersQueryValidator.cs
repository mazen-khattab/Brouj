using FluentValidation;

namespace Brouj.Application.Features.Users.Queries.GetAll;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Search).MaximumLength(100);
        RuleFor(request => request.Role).IsInEnum().When(request => request.Role is not null);
    }
}
