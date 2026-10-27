using Brouj.Domain.Enums;
using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Brouj.Infrastructure.Persistence;
using Brouj.Infrastructure.Seeder;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class InitialAdminOptionsTests
{
    [Theory]
    [InlineData("Id")]
    [InlineData("Name")]
    [InlineData("Email")]
    [InlineData("Phone")]
    [InlineData("Password")]
    [InlineData("Role")]
    public async Task Enabled_bootstrap_requires_every_approved_field_before_database_access(string missingField)
    {
        var options = ValidOptions();

        switch (missingField)
        {
            case "Id": options.Id = Guid.Empty; break;
            case "Name": options.Name = ""; break;
            case "Email": options.Email = "invalid-email"; break;
            case "Phone": options.Phone = "invalid-phone"; break;
            case "Password": options.Password = "short"; break;
            case "Role": options.Role = UserRole.Admin; break;
        }

        await using var context = TestContext.Create();
        var seeder = CreateSeeder(context, options);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(CancellationToken.None));

        Assert.False(InitialAdminOptions.IsValid(options));
        Assert.Equal("Initial administrator provisioning failed.", error.Message);
        Assert.Null(error.InnerException);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Bootstrap_is_disabled_by_default_and_does_not_connect_to_SQL()
    {
        await using var context = TestContext.Create();
        var options = new InitialAdminOptions();

        await CreateSeeder(context, options).SeedAsync(CancellationToken.None);

        Assert.False(options.Enabled);
        Assert.True(InitialAdminOptions.IsValid(options));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    internal static InitialAdminOptions ValidOptions() => new()
    {
        Enabled = true,
        Id = Guid.NewGuid(),
        Name = "Initial administrator",
        Email = "initial-admin@example.test",
        Phone = "01012345678",
        Password = "Synthetic-bootstrap-password-123!",
        Role = UserRole.SuperAdmin
    };

    private static UsersSeeding CreateSeeder(ApplicationDbContext context, InitialAdminOptions options)
    {
        return new UsersSeeding(context,
            new ApplicationTransactionManager(context, NullLogger<ApplicationTransactionManager>.Instance),
            new PasswordHasher(), Options.Create(options), NullLogger<UsersSeeding>.Instance);
    }
}
