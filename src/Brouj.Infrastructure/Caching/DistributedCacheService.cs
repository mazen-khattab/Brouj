using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics.Metrics;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Caching;
using Brouj.Application.Common.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.Caching;

/// <summary>Stores non-sensitive explicit DTOs through the host-selected distributed cache provider.</summary>
/// <remarks>Keys use a resource namespace, such as projects:list. Authenticated entries are isolated by
/// actor and role; anonymous entries use a separate scope. Invalidation addresses the caller's same scope.
/// Never cache credentials, identity documents, authorization decisions, or data needed for correctness.</remarks>
public sealed class DistributedCacheService(IDistributedCache cache, ICurrentUser actor, IOptions<CacheOptions> options) : ICacheService
{
    private const int MaximumPayloadBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions Json = new() { MaxDepth = 16 };
    private static readonly Meter Metrics = new("Brouj.Cache");
    private static readonly Counter<long> Hits = Metrics.CreateCounter<long>("brouj.cache.hits");
    private static readonly Counter<long> Misses = Metrics.CreateCounter<long>("brouj.cache.misses");

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var scopedKey = ScopedKey(key);
        var bytes = await cache.GetAsync(scopedKey, cancellationToken);
        if (bytes is null)
        {
            Misses.Add(1);
            return default;
        }

        if (bytes.Length > MaximumPayloadBytes)
        {
            await cache.RemoveAsync(scopedKey, cancellationToken);
            Misses.Add(1);
            return default;
        }

        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
            EnsureNonSensitive(document.RootElement);
            var value = JsonSerializer.Deserialize<T>(bytes, Json);

            // Counters have no cache-key/actor tags and are inert without a metrics listener.
            Hits.Add(1);
            return value;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            // Corrupt or disallowed entries are a cache miss; the authoritative source remains the database.
            await cache.RemoveAsync(scopedKey, cancellationToken);
            Misses.Add(1);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken)
    {
        var scopedKey = ScopedKey(key);
        var ttl = expiration ?? TimeSpan.FromSeconds(options.Value.DefaultExpirationSeconds);
        if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromDays(1)) throw new ArgumentOutOfRangeException(nameof(expiration));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        if (bytes.Length > MaximumPayloadBytes) throw new ArgumentException("Cache payload exceeds the allowed size.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 16 });
        EnsureNonSensitive(document.RootElement);
        await cache.SetAsync(scopedKey, bytes, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken) => cache.RemoveAsync(ScopedKey(key), cancellationToken);

    private string ScopedKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200 || !key.Contains(':') ||
            key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ':' and not '-' and not '_' and not '.'))
            throw new ArgumentException("Use a bounded resource:key cache namespace.", nameof(key));
        var scope = "anonymous";
        if (actor.IsAuthenticated)
        {
            if (actor.CustomerId is { } customer && customer != Guid.Empty && actor.UserId is null && actor.Role is null)
                scope = "customer:" + customer.ToString("N");
            else if (actor.UserId is { } user && user != Guid.Empty && actor.CustomerId is null && actor.Role is { } role && Enum.IsDefined(role))
                scope = "user:" + user.ToString("N") + ":" + (int)role;
            else throw new InvalidOperationException("Cache requires a valid actor scope.");
        }
        // Hashing keeps physical keys bounded and conceals actor IDs, but is not permission to put secrets
        // in logical keys. Callers must invalidate their scope after changes; short TTL bounds stale reads.
        return "brouj:v1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope + ":" + key)));
    }

    private static void EnsureNonSensitive(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject())
            {
                var name = property.Name.Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
                if (name.Contains("password") || name.Contains("token") || name.Contains("secret") || name.Contains("cookie") ||
                    name.Contains("identitynumber") || name.Contains("signingkey"))
                    throw new ArgumentException("Sensitive data cannot be cached.");
                EnsureNonSensitive(property.Value);
            }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) EnsureNonSensitive(child);
    }
}
