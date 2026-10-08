using Brouj.Domain.Enums;

namespace Brouj.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    Guid? CustomerId { get; }
    UserRole? Role { get; }
}
