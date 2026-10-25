using FluentValidation;

namespace Brouj.Application.Features.Reservations.Queries.GetMine;

public sealed class GetMyReservationsQueryValidator : AbstractValidator<GetMyReservationsQuery>
{
    public GetMyReservationsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Status).IsInEnum().When(request => request.Status is not null);
    }
}
