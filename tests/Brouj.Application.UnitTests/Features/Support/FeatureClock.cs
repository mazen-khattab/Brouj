namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
}
