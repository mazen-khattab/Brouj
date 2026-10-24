using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Enums;

namespace Brouj.Infrastructure.IntegrationTests.Fixtures;

public sealed record TestActor(
    bool IsAuthenticated = false,
    Guid? UserId = null,
    Guid? CustomerId = null,
    UserRole? Role = null) : ICurrentUser
{
    public static TestActor Staff(UserRole role = UserRole.Admin) => new(true, Guid.NewGuid(), null, role);
    public static TestActor Customer() => new(true, null, Guid.NewGuid(), null);
}
