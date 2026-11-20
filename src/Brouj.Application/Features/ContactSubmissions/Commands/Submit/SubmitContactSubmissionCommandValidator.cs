using Brouj.Application.Common.Utils;
using FluentValidation;

namespace Brouj.Application.Features.ContactSubmissions.Commands.Submit;

public sealed class SubmitContactSubmissionCommandValidator : AbstractValidator<SubmitContactSubmissionCommand>
{
    public SubmitContactSubmissionCommandValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.Input).NotNull().DependentRules(() =>
        {
            RuleFor(request => request.Input.Name).NotEmpty().MaximumLength(150);
            RuleFor(request => request.Input.Phone).EgyptianPhone()
                .WithMessage("Enter an 11-digit Egyptian mobile number starting with 010, 011, 012, or 015.");
            RuleFor(request => request.Input.Inquiry).NotEmpty().MaximumLength(2000);
        });
    }
}
