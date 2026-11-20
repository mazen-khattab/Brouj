using FluentValidation;

namespace Brouj.Application.Features.ContactSubmissions.Queries.GetAll;

public sealed class GetContactSubmissionsQueryValidator : AbstractValidator<GetContactSubmissionsQuery>
{
    public GetContactSubmissionsQueryValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
