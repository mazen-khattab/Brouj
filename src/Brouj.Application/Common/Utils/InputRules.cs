using FluentValidation;
namespace Brouj.Application.Common.Utils;

public static class InputRules
{
    public static IRuleBuilderOptions<T, string> EgyptianPhone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches(new System.Text.RegularExpressions.Regex(@"\A01[0125][0-9]{8}\z",
            System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100)));

    public static IRuleBuilderOptions<T, string> NationalId<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches(new System.Text.RegularExpressions.Regex(@"\A[0-9]{14}\z",
            System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(100)));

    public static IRuleBuilderOptions<T, string> Email<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(254).Must(value =>
            value is not null && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(value.Trim()))
            .WithMessage("A valid email address is required.");

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
