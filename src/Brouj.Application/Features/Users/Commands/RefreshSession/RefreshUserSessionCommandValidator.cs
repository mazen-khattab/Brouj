using FluentValidation;

namespace Brouj.Application.Features.Users.Commands.RefreshSession;

public sealed class RefreshUserSessionCommandValidator : AbstractValidator<RefreshUserSessionCommand>
{
    public RefreshUserSessionCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // No client-writable fields exist for this operation.
    }
}
