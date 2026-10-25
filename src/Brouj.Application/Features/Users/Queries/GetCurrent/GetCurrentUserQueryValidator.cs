using FluentValidation;

namespace Brouj.Application.Features.Users.Queries.GetCurrent;

public sealed class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
