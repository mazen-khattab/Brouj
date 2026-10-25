using FluentValidation;

namespace Brouj.Application.Features.Reservations.Queries.GetDetails;

public sealed class GetReservationDetailsQueryValidator : AbstractValidator<GetReservationDetailsQuery>
{
    public GetReservationDetailsQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
