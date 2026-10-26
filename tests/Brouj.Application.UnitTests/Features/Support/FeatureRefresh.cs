using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureRefresh : IRefreshTokenService
{
    public Guid ActorId { get; set; }

    public bool Reject { get; set; }

    public bool Revoked { get; private set; }

    public Task<RefreshTokenResult?> CreateCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        CreateAsync(customerId, cancellationToken);

    public Task<RefreshTokenResult?> CreateUserAsync(Guid userId, CancellationToken cancellationToken) =>
        CreateAsync(userId, cancellationToken);

    public Task<RefreshTokenResult?> RotateCustomerAsync(string token, CancellationToken cancellationToken) =>
        CreateAsync(ActorId, cancellationToken);

    public Task<RefreshTokenResult?> RotateUserAsync(string token, CancellationToken cancellationToken) =>
        CreateAsync(ActorId, cancellationToken);

    public Task<bool> RevokeCustomerAsync(string token, CancellationToken cancellationToken) => RevokeAsync();

    public Task<bool> RevokeUserAsync(string token, CancellationToken cancellationToken) => RevokeAsync();

    private Task<RefreshTokenResult?> CreateAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<RefreshTokenResult?>(Reject
            ? null
            : new RefreshTokenResult(id.ToString(), "synthetic-refresh", new FeatureClock().GetUtcNow().AddDays(7)));
    }

    private Task<bool> RevokeAsync()
    {
        Revoked = true;

        return Task.FromResult(true);
    }
}
