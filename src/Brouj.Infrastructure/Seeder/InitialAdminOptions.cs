using Brouj.Application.Features.Users.DTOs;
using Brouj.Domain.Enums;

namespace Brouj.Infrastructure.Seeder;

/// <summary>Explicit, private bootstrap settings supplied by user-secrets or a deployment secret provider.</summary>
public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";
    public bool Enabled { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public string Password { get; set; } = string.Empty;

    public static bool IsValid(InitialAdminOptions options)
    {

        if (!options.Enabled)
        {
            return true;
        }

        if (options.Id == Guid.Empty || options.Role != UserRole.SuperAdmin)
        {
            return false;
        }

        // Reuse the approved staff validation rules rather than inventing bootstrap-specific limits.
        var input = new CreateUserInput
        {
            Name = options.Name,
            Email = options.Email,
            Phone = options.Phone,
            Role = options.Role.Value,
            Password = options.Password
        };

        return new CreateUserInputValidator().Validate(input).IsValid;
    }
}
