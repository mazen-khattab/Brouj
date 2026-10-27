using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brouj.API.Diagnostics;

/// <summary>Opt-in, value-free diagnostics for repeatable benchmarks; exposes no HTTP endpoint.</summary>
public sealed class PerformanceDiagnosticsService(ILogger<PerformanceDiagnosticsService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var process = Process.GetCurrentProcess();
        using var observer = new DatabaseObserver();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(observer);
        using var connections = new ConnectionCounters();
        using var cache = new MeterListener();
        long hits = 0;
        long misses = 0;

        cache.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Brouj.Cache")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        cache.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "brouj.cache.hits")
            {
                Interlocked.Add(ref hits, measurement);
            }
            else if (instrument.Name == "brouj.cache.misses")
            {
                Interlocked.Add(ref misses, measurement);
            }
        });
        cache.Start();

        var previousCpu = process.TotalProcessorTime;
        var elapsed = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        logger.LogInformation("Performance diagnostics enabled; SQL text, parameters and identities are excluded");

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                process.Refresh();
                var cpu = process.TotalProcessorTime;
                var seconds = elapsed.Elapsed.TotalSeconds;
                var memory = GC.GetGCMemoryInfo();
                var sample = new
                {
                    Utc = DateTimeOffset.UtcNow,
                    CpuPercent = (cpu - previousCpu).TotalSeconds / seconds / Environment.ProcessorCount * 100,
                    LogicalProcessors = Environment.ProcessorCount,
                    WorkingSetBytes = process.WorkingSet64,
                    PrivateBytes = process.PrivateMemorySize64,
                    ManagedBytes = GC.GetTotalMemory(false),
                    TotalAllocatedBytes = GC.GetTotalAllocatedBytes(),
                    HeapBytes = memory.HeapSizeBytes,
                    FragmentedBytes = memory.FragmentedBytes,
                    GcPausePercent = memory.PauseTimePercentage,
                    Gen0 = GC.CollectionCount(0),
                    Gen1 = GC.CollectionCount(1),
                    Gen2 = GC.CollectionCount(2),
                    ThreadPoolThreads = ThreadPool.ThreadCount,
                    ThreadPoolQueue = ThreadPool.PendingWorkItemCount,
                    ThreadPoolCompleted = ThreadPool.CompletedWorkItemCount,
                    Database = observer.Snapshot(),
                    Connections = connections.Snapshot(),
                    CacheHits = Interlocked.Read(ref hits),
                    CacheMisses = Interlocked.Read(ref misses)
                };

                // A fixed marker lets the runner extract only this sanitized JSON from normal host logs.
                logger.LogInformation("BROUJ_PERFORMANCE {Sample}", JsonSerializer.Serialize(sample));
                previousCpu = cpu;
                elapsed.Restart();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Performance diagnostics stopped");
        }
    }

    private sealed class DatabaseObserver : IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions = [];
        private readonly long[] _milliseconds = new long[5002];
        private long _errors;

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
            {
                _subscriptions.Add(listener.Subscribe(this, name =>
                    name == RelationalEventId.CommandExecuted.Name || name == RelationalEventId.CommandError.Name));
            }
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Value is CommandExecutedEventData command)
            {
                var bucket = Math.Clamp((int)Math.Ceiling(command.Duration.TotalMilliseconds), 0, 5001);
                Interlocked.Increment(ref _milliseconds[bucket]);
            }
            else if (value.Key == RelationalEventId.CommandError.Name)
            {
                Interlocked.Increment(ref _errors);
            }
        }

        public object Snapshot()
        {
            var buckets = new long[_milliseconds.Length];

            for (var index = 0; index < buckets.Length; index++)
            {
                buckets[index] = Interlocked.Exchange(ref _milliseconds[index], 0);
            }

            var count = buckets.Sum();

            int? Percentile(double percentile)
            {
                long accumulated = 0;

                for (var index = 0; index < buckets.Length && count > 0; index++)
                {
                    accumulated += buckets[index];

                    if (accumulated >= Math.Ceiling(count * percentile))
                    {
                        return index;
                    }
                }

                return null;
            }

            return new { Count = count, P50Ms = Percentile(.5), P95Ms = Percentile(.95),
                P99Ms = Percentile(.99), Errors = Interlocked.Exchange(ref _errors, 0), OverflowMs = 5001 };
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }

    private sealed class ConnectionCounters : EventListener
    {
        // EventListener invokes OnEventSourceCreated during its base constructor.
        private ConcurrentDictionary<string, double>? _values;
        private ConcurrentDictionary<string, double> Values =>
            LazyInitializer.EnsureInitialized(ref _values, () => new ConcurrentDictionary<string, double>());

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft.Data.SqlClient.EventSource")
            {
                EnableEvents(source, EventLevel.Informational, EventKeywords.None,
                    new Dictionary<string, string?> { ["EventCounterIntervalSec"] = "5" });
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs args)
        {
            if (args.EventName != "EventCounters" || args.Payload?[0] is not IDictionary<string, object> values ||
                !values.TryGetValue("Name", out var name) || name is not string counter)
            {
                return;
            }

            // Fixed provider counter names only; never forward diagnostic strings or connection details.
            if (counter is "active-hard-connections" or "active-soft-connects" or "number-of-pooled-connections" or
                "number-of-active-connections" or "number-of-free-connections" or "number-of-reclaimed-connections" or
                "hard-connects" or "soft-connects")
            {
                values.TryGetValue("Increment", out var increment);
                var value = values.TryGetValue("Mean", out var mean) ? mean : increment;

                if (value is not null)
                {
                    Values[counter] = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
        }

        public Dictionary<string, double> Snapshot() => new(Values);
    }
}
