using FluentValidation;

namespace Brouj.Application.Features.Users.Queries.GetById;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Id).NotEmpty();
    }
}
