using FluentValidation;

namespace Brouj.Application.Common.DTOs;

public sealed class UploadInputValidator : AbstractValidator<UploadInput>
{
    public UploadInputValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(input => input.Content).NotNull().Must(stream => stream is not null && stream.CanRead);
        RuleFor(input => input.FileName).NotEmpty().MaximumLength(255);
        RuleFor(input => input.ContentType).NotEmpty().MaximumLength(100);
        RuleFor(input => input.Length).InclusiveBetween(1, 10 * 1024 * 1024);
        // The storage adapter validates actual bytes, type signatures and its configured size limit.
    }
}
