using Brouj.Application.Common.Models;

namespace Brouj.Application.Abstractions.Authentication;

/// <summary>Manages opaque refresh credentials after the caller has authenticated a login.</summary>
/// <remarks>Actor IDs for creation must come from the verified login, never an untrusted request body.
/// Rotation and logout require possession of a token and never accept an actor ID.</remarks>
public interface IRefreshTokenService
{
    /// <summary>Starts the customer's sole active session, revoking any previous refresh credential.</summary>
    Task<RefreshTokenResult?> CreateCustomerAsync(Guid customerId, CancellationToken cancellationToken);
    /// <summary>Starts the staff member's sole active session after a verified login.</summary>
    Task<RefreshTokenResult> CreateUserAsync(Guid userId, CancellationToken cancellationToken);
    /// <summary>Consumes a customer credential once, preserving the original session deadline.</summary>
    Task<RefreshTokenResult?> RotateCustomerAsync(string token, CancellationToken cancellationToken);
    /// <summary>Consumes a staff credential once, preserving the original session deadline.</summary>
    Task<RefreshTokenResult?> RotateUserAsync(string token, CancellationToken cancellationToken);
    /// <summary>Revokes only the presented active customer credential; stale tokens cannot revoke a replacement.</summary>
    Task<bool> RevokeCustomerAsync(string token, CancellationToken cancellationToken);
    /// <summary>Revokes only the presented active staff credential.</summary>
    Task<bool> RevokeUserAsync(string token, CancellationToken cancellationToken);
}
