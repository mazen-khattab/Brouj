using System.Collections.Concurrent;
using Brouj.Application.Common.Settings;
using Brouj.Infrastructure.Caching;
using Brouj.Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Brouj.Infrastructure.IntegrationTests.Services;

public sealed class CacheTests
{
    [Fact]
    public async Task Cache_round_trip_default_ttl_invalidation_and_deterministic_scoping()
    {
        var provider = new RecordingCache();
        var actor = TestActor.Customer();
        var first = Service(provider, actor);
        var second = Service(provider, actor);
        await first.SetAsync("projects:list", new CachedProject("project"), null, default);
        Assert.Equal(TimeSpan.FromMinutes(5), provider.LastOptions!.AbsoluteExpirationRelativeToNow);
        Assert.Equal(new CachedProject("project"), await second.GetAsync<CachedProject>("projects:list", default));
        Assert.Null(await Service(provider, TestActor.Customer()).GetAsync<CachedProject>("projects:list", default));
        Assert.Null(await Service(provider, new TestActor()).GetAsync<CachedProject>("projects:list", default));
        Assert.Null(await Service(provider, TestActor.Staff()).GetAsync<CachedProject>("projects:list", default));
        Assert.DoesNotContain(actor.CustomerId!.Value.ToString("N"), Assert.Single(provider.Values.Keys));
        await second.RemoveAsync("projects:list", default);
        Assert.Null(await first.GetAsync<CachedProject>("projects:list", default));
    }

    [Fact]
    public async Task Staff_role_changes_do_not_reuse_a_previous_privilege_scope()
    {
        var provider = new RecordingCache();
        var actor = TestActor.Staff();
        await Service(provider, actor).SetAsync("projects:list", new CachedProject("project"), null, default);
        Assert.Null(await Service(provider, actor with { Role = Brouj.Domain.Enums.UserRole.SuperAdmin }).GetAsync<CachedProject>("projects:list", default));
    }

    [Fact]
    public async Task Sensitive_payloads_invalid_keys_and_unbounded_expiration_are_rejected()
    {
        var provider = new RecordingCache();
        var service = Service(provider, new TestActor());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetAsync("auth:session", new { Nested = new { RefreshToken = "synthetic" } }, null, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetAsync("https://unsafe?value=secret", new CachedProject("project"), null, default));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.SetAsync("projects:list", new CachedProject("project"), TimeSpan.Zero, default));
        Assert.Empty(provider.Values);
    }

    [Fact]
    public async Task Corrupt_payload_is_invalidated_and_cancellation_is_forwarded()
    {
        var provider = new RecordingCache();
        var service = Service(provider, new TestActor());
        await service.SetAsync("projects:list", new CachedProject("project"), TimeSpan.FromMinutes(1), default);
        provider.Values[Assert.Single(provider.Values.Keys)] = "not-json"u8.ToArray();
        Assert.Null(await service.GetAsync<CachedProject>("projects:list", default));
        Assert.Empty(provider.Values);
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync<CachedProject>("projects:list", source.Token));
    }

    private static DistributedCacheService Service(RecordingCache provider, TestActor actor) => new(provider, actor, Options.Create(new CacheOptions()));
    private sealed record CachedProject(string Name);

    // Provider double verifies the distributed contract only; it does not replace SQL relational tests.
    internal sealed class RecordingCache : IDistributedCache
    {
        internal ConcurrentDictionary<string, byte[]> Values { get; } = new();
        internal DistributedCacheEntryOptions? LastOptions { get; private set; }
        public byte[]? Get(string key) => Values.GetValueOrDefault(key);
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) { token.ThrowIfCancellationRequested(); return Task.FromResult(Get(key)); }
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) { LastOptions = options; Values[key] = value; }
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { token.ThrowIfCancellationRequested(); Set(key, value, options); return Task.CompletedTask; }
        public void Remove(string key) => Values.TryRemove(key, out _);
        public Task RemoveAsync(string key, CancellationToken token = default) { token.ThrowIfCancellationRequested(); Remove(key); return Task.CompletedTask; }
        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) { token.ThrowIfCancellationRequested(); return Task.CompletedTask; }
    }
}
