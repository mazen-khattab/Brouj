using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Enums;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureActor : ICurrentUser
{
    public bool IsAuthenticated { get; set; }

    public Guid? UserId { get; set; }

    public Guid? CustomerId { get; set; }

    public UserRole? Role { get; set; }
}
