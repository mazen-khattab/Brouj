using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.Performance;

/// <summary>Open-arrival load with bounded workers/queue; dropped work is visible, never silently slowed down.</summary>
internal sealed class LoadPhase(BaselineEnvironment environment, ILogger<LoadPhase> logger)
{
    public static readonly string[] Scenarios = ["Cities", "Areas", "ProjectList", "ProjectDetails", "UnitList",
        "UnitDetails", "CurrentTerms", "CustomerReads", "ReservationWrites", "AdminReads"];
    private long _writeSequence;

    public static int MixedScenario(long sequence)
    {
        var slot = (int)(sequence % 100);

        if (slot < 80)
        {
            return (int)(sequence % 7);
        }

        if (slot < 90)
        {
            return 7;
        }

        return slot < 95 ? 9 : 8;
    }

    public async Task<PhaseResult> RunAsync(string name, int seconds, int rate, int? scenario,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        var metrics = Scenarios.Select(_ => new RequestMeasurements()).ToArray();
        var queue = Channel.CreateBounded<Job>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true
        });
        using var anonymous = environment.NewClient();
        long offered = 0;
        long dropped = 0;
        long active = 0;
        long peak = 0;
        double maximumSchedulerDelay = 0;

        async Task WorkerAsync()
        {
            await foreach (var job in queue.Reader.ReadAllAsync(cancellationToken))
            {
                var inFlight = Interlocked.Increment(ref active);
                long observed;

                do
                {
                    observed = Interlocked.Read(ref peak);
                }
                while (inFlight > observed && Interlocked.CompareExchange(ref peak, inFlight, observed) != observed);

                try
                {
                    await SendAsync(job, anonymous, metrics[job.Scenario], cancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref active);
                }
            }
        }

        var workers = Enumerable.Range(0, 256).Select(_ => WorkerAsync()).ToArray();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        var nextProgress = 10;
        logger.LogInformation("Phase {Phase}: {Rate} offered RPS for {Seconds}s", name, rate, seconds);

        while (clock.Elapsed.TotalSeconds < seconds && await timer.WaitForNextTickAsync(cancellationToken))
        {
            var elapsed = Math.Min(clock.Elapsed.TotalSeconds, seconds);
            var due = (long)Math.Floor(elapsed * rate);

            while (offered < due)
            {
                var requestScenario = scenario ?? MixedScenario(offered);
                var scheduled = started.AddSeconds((double)offered / rate);
                maximumSchedulerDelay = Math.Max(maximumSchedulerDelay,
                    (DateTimeOffset.UtcNow - scheduled).TotalMilliseconds);
                var job = new Job(offered, requestScenario, scheduled);

                if (!queue.Writer.TryWrite(job))
                {
                    dropped++;
                }

                offered++;
            }

            if (elapsed >= nextProgress)
            {
                logger.LogInformation("Phase {Phase}: elapsed {Elapsed}s, completed {Completed}, dropped {Dropped}",
                    name, (int)elapsed, metrics.Sum(value => value.Completed), dropped);
                nextProgress += 10;
            }
        }

        queue.Writer.Complete();
        await Task.WhenAll(workers);
        var actualSeconds = clock.Elapsed.TotalSeconds;
        var summaries = metrics.Select((value, index) => value.Summary(Scenarios[index])).Where(value => value.Count > 0)
            .ToArray();
        var aggregate = RequestMeasurements.Combine(metrics);
        var result = new PhaseResult(name, started, DateTimeOffset.UtcNow, seconds, actualSeconds, rate,
            offered, dropped, peak, maximumSchedulerDelay, aggregate.Summary("All"), summaries);
        logger.LogInformation("Phase {Phase} finished: {Rps:F1} successful RPS; p95 {P95}ms; p99 {P99}ms; errors {Errors}; rejects {Rejects}",
            name, result.SuccessfulRps, result.Total.P95Ms, result.Total.P99Ms,
            result.Total.UnexpectedErrors, result.Total.RateLimitRejects);

        return result;
    }

    private async Task SendAsync(Job job, HttpClient anonymous, RequestMeasurements metrics,
        CancellationToken cancellationToken)
    {
        var unit = environment.Units[(int)(job.Sequence * 7919 % environment.Units.Count)];
        var actorIndex = (int)(job.Sequence % environment.Sessions.Count);
        var session = job.Scenario == 9 ? environment.Staff : environment.Sessions[actorIndex];
        var route = job.Scenario switch
        {
            0 => "/api/v1/cities?pageNumber=1&pageSize=20",
            1 => $"/api/v1/areas?pageNumber={job.Sequence % 5 + 1}&pageSize=20",
            2 => $"/api/v1/projects?pageNumber={job.Sequence % 50 + 1}&pageSize=20",
            3 => $"/api/v1/projects/{unit.Project}",
            4 => $"/api/v1/projects/{unit.Project}/units?pageNumber={job.Sequence % 5 + 1}&pageSize=20",
            5 => $"/api/v1/units/{unit.Unit}?projectId={unit.Project}",
            6 => "/api/v1/terms-and-conditions/current",
            7 => "/api/v1/reservations/me?pageNumber=1&pageSize=20",
            8 => "/api/v1/reservations",
            9 => $"/api/v1/admin/reservations?pageNumber={job.Sequence % 50 + 1}&pageSize=20",
            _ => throw new ArgumentOutOfRangeException(nameof(job))
        };
        using var request = new HttpRequestMessage(job.Scenario == 8 ? HttpMethod.Post : HttpMethod.Get, route);

        if (job.Scenario == 8)
        {
            var sequence = Interlocked.Increment(ref _writeSequence) - 1;
            var capacity = (long)environment.Units.Count * environment.Sessions.Count;

            if (sequence >= capacity)
            {
                throw new InvalidOperationException("Unique reservation fixture pairs exhausted; reseed before another run.");
            }

            // Every write has a fresh (customer, unit) pair. Conflicts cannot masquerade as successful writes.
            session = environment.Sessions[(int)(sequence / environment.Units.Count)];
            request.Content = JsonContent.Create(new { unitId = environment.Units[(int)(sequence % environment.Units.Count)].Unit });
            request.Headers.Add("X-CSRF-TOKEN", session.Credentials.Csrf);
        }

        if (job.Scenario >= 7)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Credentials.Token);
        }

        var client = job.Scenario >= 7 ? session.Client : anonymous;
        var status = 0;
        long bytes = 0;

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            bytes = (await response.Content.ReadAsByteArrayAsync(cancellationToken)).Length;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Network failures and the finite HTTP timeout are counted without logging exception payloads.
        }

        metrics.Record(status, job.Scenario == 8 ? 201 : 200,
            (DateTimeOffset.UtcNow - job.Scheduled).TotalMilliseconds, bytes);
    }

    private sealed record Job(long Sequence, int Scenario, DateTimeOffset Scheduled);
}

/// <summary>Bounded 1ms histogram, including scheduling and queue time to avoid coordinated omission.</summary>
internal sealed class RequestMeasurements
{
    private readonly long[] _latency = new long[60002];
    private long _completed;
    private long _successful;
    private long _unexpected;
    private long _rejected;
    private long _bytes;
    private long _maximumBytes;
    private readonly long[] _statuses = new long[600];
    public long Completed => Interlocked.Read(ref _completed);

    public void Record(int status, int expected, double milliseconds, long bytes)
    {
        Interlocked.Increment(ref _completed);
        Interlocked.Increment(ref _latency[Math.Clamp((int)Math.Ceiling(milliseconds), 0, 60001)]);
        Interlocked.Increment(ref _statuses[Math.Clamp(status, 0, 599)]);
        Interlocked.Add(ref _bytes, bytes);
        long previous;

        do
        {
            previous = Interlocked.Read(ref _maximumBytes);
        }
        while (bytes > previous && Interlocked.CompareExchange(ref _maximumBytes, bytes, previous) != previous);

        if (status == expected)
        {
            Interlocked.Increment(ref _successful);
        }
        else if (status == 429)
        {
            Interlocked.Increment(ref _rejected);
        }
        else
        {
            Interlocked.Increment(ref _unexpected);
        }
    }

    public RequestSummary Summary(string name)
    {
        int? Percentile(double percentile)
        {
            long accumulated = 0;

            for (var index = 0; index < _latency.Length && _completed > 0; index++)
            {
                accumulated += _latency[index];

                if (accumulated >= Math.Ceiling(_completed * percentile))
                {
                    return index;
                }
            }

            return null;
        }

        return new RequestSummary(name, _completed, _successful, _unexpected, _rejected,
            Percentile(.5), Percentile(.95), Percentile(.99), _completed == 0 ? 0 : (double)_bytes / _completed,
            _maximumBytes, _statuses.Select((count, status) => (count, status)).Where(value => value.count > 0)
                .ToDictionary(value => value.status, value => value.count));
    }

    public static RequestMeasurements Combine(IEnumerable<RequestMeasurements> measurements)
    {
        var result = new RequestMeasurements();

        foreach (var value in measurements)
        {
            result._completed += value._completed;
            result._successful += value._successful;
            result._unexpected += value._unexpected;
            result._rejected += value._rejected;
            result._bytes += value._bytes;
            result._maximumBytes = Math.Max(result._maximumBytes, value._maximumBytes);

            for (var index = 0; index < result._latency.Length; index++)
            {
                result._latency[index] += value._latency[index];
            }

            for (var index = 0; index < result._statuses.Length; index++)
            {
                result._statuses[index] += value._statuses[index];
            }
        }

        return result;
    }
}

internal sealed record RequestSummary(string Name, long Count, long Successful, long UnexpectedErrors,
    long RateLimitRejects, int? P50Ms, int? P95Ms, int? P99Ms, double MeanPayloadBytes, long MaximumPayloadBytes,
    Dictionary<int, long> Statuses);

internal sealed record PhaseResult(string Name, DateTimeOffset StartedUtc, DateTimeOffset FinishedUtc,
    int ScheduledSeconds, double ActualSeconds, int OfferedRps, long Offered, long Dropped, long PeakConcurrency,
    double MaximumSchedulerDelayMs, RequestSummary Total, RequestSummary[] Scenarios)
{
    public double SuccessfulRps => Total.Successful / ActualSeconds;
    public double CompletedRps => Total.Count / ActualSeconds;
    public double UnexpectedErrorRate => Total.Count == 0 ? 1 : (double)Total.UnexpectedErrors / Total.Count;
    public bool Stable => Total.Count > 0 && Dropped == 0 && Total.RateLimitRejects == 0 &&
        UnexpectedErrorRate < .01 && Total.P95Ms <= 200 && Total.P99Ms <= 500 && SuccessfulRps >= OfferedRps * .99;
}
