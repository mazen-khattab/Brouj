namespace Brouj.Application.Common.Settings;

public sealed class RateLimitOptions
{
    public RateLimitPolicyOptions PublicReads { get; init; } = new();
    public RateLimitPolicyOptions Authentication { get; init; } = new();
    public RateLimitPolicyOptions ReservationWrites { get; init; } = new();
    public RateLimitPolicyOptions AdminWrites { get; init; } = new();
    public RateLimitPolicyOptions FileUploads { get; init; } = new();
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; }
    public int WindowSeconds { get; init; }
}
