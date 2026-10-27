namespace Brouj.Infrastructure.IntegrationTests.Performance;

public sealed class MeasurementTests
{
    [Fact]
    public void Mixed_traffic_has_the_approved_exact_category_weights()
    {
        var choices = Enumerable.Range(0, 700).Select(index => LoadPhase.MixedScenario(index)).ToArray();

        Assert.Equal(560, choices.Count(value => value < 7));
        Assert.Equal(70, choices.Count(value => value == 7));
        Assert.Equal(35, choices.Count(value => value == 9));
        Assert.Equal(35, choices.Count(value => value == 8));
        Assert.All(Enumerable.Range(0, 7), scenario => Assert.Equal(80, choices.Count(value => value == scenario)));
    }

    [Fact]
    public void Errors_and_rejects_are_separate_and_latency_includes_failed_requests()
    {
        var measurements = new RequestMeasurements();
        measurements.Record(200, 200, 2.1, 100);
        measurements.Record(429, 200, 100, 200);
        measurements.Record(409, 201, 300, 300);
        measurements.Record(0, 200, 5000, 0);
        var summary = measurements.Summary("test");

        Assert.Equal(4, summary.Count);
        Assert.Equal(1, summary.Successful);
        Assert.Equal(1, summary.RateLimitRejects);
        Assert.Equal(2, summary.UnexpectedErrors);
        Assert.Equal(100, summary.P50Ms);
        Assert.Equal(5000, summary.P95Ms);
        Assert.Equal(5000, summary.P99Ms);
        Assert.Equal(150, summary.MeanPayloadBytes);
    }

    [Fact]
    public void Aggregate_percentiles_merge_histograms_instead_of_averaging_percentiles()
    {
        var quick = new RequestMeasurements();
        var slow = new RequestMeasurements();

        for (var index = 0; index < 99; index++)
        {
            quick.Record(200, 200, 10, 10);
        }

        slow.Record(201, 201, 1000, 30);
        var result = RequestMeasurements.Combine([quick, slow]).Summary("all");

        Assert.Equal(100, result.Count);
        Assert.Equal(10, result.P95Ms);
        Assert.Equal(10, result.P99Ms);
        Assert.Equal(100, result.Successful);
        Assert.Equal(30, result.MaximumPayloadBytes);
    }

    [Fact]
    public void Empty_or_dropped_load_cannot_be_reported_as_stable()
    {
        var measurements = new RequestMeasurements();
        var empty = new PhaseResult("empty", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 1, 1, 1,
            1, 0, 1, 0, measurements.Summary("all"), []);
        Assert.False(empty.Stable);

        measurements.Record(200, 200, 1, 1);
        var dropped = empty with { Total = measurements.Summary("all"), Dropped = 1 };
        Assert.False(dropped.Stable);
        Assert.True((dropped with { Dropped = 0 }).Stable);
    }
}
