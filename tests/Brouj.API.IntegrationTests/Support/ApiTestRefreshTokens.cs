using System.Security.Cryptography;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Domain.Enums;

namespace Brouj.API.IntegrationTests.Support;

/// <summary>A transport/session double. Real refresh locking, expiry and SQL rotation remain pending.</summary>
internal sealed class ApiTestRefreshTokens : IRefreshTokenService
{
    private readonly Dictionary<string, (Guid Id, bool Staff)> _tokens = new();
    public IJwtTokenService Jwt { get; set; } = null!;

    public Task<RefreshTokenResult?> CreateCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        Task.FromResult<RefreshTokenResult?>(Issue(customerId, false));

    public Task<RefreshTokenResult?> CreateUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<RefreshTokenResult?>(Issue(userId, true));

    public Task<RefreshTokenResult?> RotateCustomerAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Rotate(token, false));

    public Task<RefreshTokenResult?> RotateUserAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Rotate(token, true));

    public Task<bool> RevokeCustomerAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Revoke(token, false));

    public Task<bool> RevokeUserAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Revoke(token, true));

    private RefreshTokenResult Issue(Guid id, bool staff)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _tokens[token] = (id, staff);

        return new RefreshTokenResult(staff ? Jwt.GenerateUserAccessToken(id, UserRole.Admin)
            : Jwt.GenerateCustomerAccessToken(id), token, DateTimeOffset.UtcNow.AddHours(1));
    }

    private RefreshTokenResult? Rotate(string token, bool staff)
    {
        if (!_tokens.TryGetValue(token, out var actor) || actor.Staff != staff)
        {
            return null;
        }

        _tokens.Remove(token);

        return Issue(actor.Id, staff);
    }

    private bool Revoke(string token, bool staff) =>
        _tokens.TryGetValue(token, out var actor) && actor.Staff == staff && _tokens.Remove(token);
}
