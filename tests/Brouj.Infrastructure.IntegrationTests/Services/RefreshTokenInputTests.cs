using Brouj.Infrastructure.Authentication;
using Brouj.Infrastructure.Persistence;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class RefreshTokenInputTests
{
    [Theory]
    [InlineData("")]
    [InlineData("not-a-token")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public async Task Invalid_credentials_are_rejected_without_opening_a_database(string raw)
    {
        var service = new RefreshTokenService(new ForbiddenFactory(), AuthenticationTests.Service(), new FixedClock(AuthenticationTests.Now));
        Assert.Null(await service.RotateCustomerAsync(raw, default));
        Assert.Null(await service.RotateUserAsync(raw, default));
        Assert.False(await service.RevokeCustomerAsync(raw, default));
        Assert.False(await service.RevokeUserAsync(raw, default));
        Assert.Null(await service.CreateCustomerAsync(Guid.Empty, default));
        Assert.Null(await service.CreateUserAsync(Guid.Empty, default));
    }

    private sealed class ForbiddenFactory : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => throw new InvalidOperationException("Malformed credentials must not query SQL Server.");
    }
}
