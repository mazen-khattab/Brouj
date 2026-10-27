using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.Performance;

public sealed class LocalPerformanceBaseline
{
    [PerformanceBaselineFact]
    public async Task Execute_approved_local_baseline()
    {
        var root = Environment.GetEnvironmentVariable("BROUJ_BASELINE_ROOT")
            ?? throw new InvalidOperationException("BROUJ_BASELINE_ROOT must identify the repository root.");
        var output = Environment.GetEnvironmentVariable("BROUJ_BASELINE_OUTPUT")
            ?? throw new InvalidOperationException("BROUJ_BASELINE_OUTPUT must identify a new artifact directory.");
        Directory.CreateDirectory(output);
        using var logging = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        var logger = logging.CreateLogger<LocalPerformanceBaseline>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromHours(3));
        var cancellationToken = timeout.Token;
        await using var environment = new BaselineEnvironment(logging.CreateLogger<BaselineEnvironment>());
        await environment.StartAsync(root, output, cancellationToken);
        var phase = new LoadPhase(environment, logging.CreateLogger<LoadPhase>());
        var evidence = new SqlEvidence(environment, logging.CreateLogger<SqlEvidence>());
        using var sampling = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sampler = evidence.SampleAsync(output, sampling.Token);
        var results = new List<PhaseResult>();
        var smoke = Environment.GetEnvironmentVariable("BROUJ_BASELINE_SMOKE") == "1";

        async Task<PhaseResult> RunAsync(string name, int seconds, int rate, int? scenario = null)
        {
            var result = await phase.RunAsync(name, smoke ? 2 : seconds, rate, scenario, cancellationToken);
            results.Add(result);
            await File.WriteAllTextAsync(Path.Combine(output, "phases.json"),
                JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

            return result;
        }

        try
        {
            await RunAsync("Warmup", 30, 25);

            // Calibrate conservatively before selecting the sustained load/soak arrival rate.
            var stableRate = 10;
            foreach (var rate in new[] { 25, 50, 100, 200, 400 })
            {
                var result = await RunAsync($"Calibration-{rate}", 20, rate);

                if (result.Stable)
                {
                    stableRate = rate;
                }
            }

            for (var index = 0; index < LoadPhase.Scenarios.Length; index++)
            {
                await RunAsync("Separate-" + LoadPhase.Scenarios[index], 30, Math.Min(50, stableRate), index);
            }

            await environment.RenewSessionsAsync(cancellationToken);
            await RunAsync("Load", 300, stableRate);
            await environment.RenewSessionsAsync(cancellationToken);

            foreach (var rate in new[] { 500, 1000, 2000, 4000, 8000, 10000 })
            {
                await RunAsync($"Stress-{rate}", 100, rate);
            }

            await environment.RenewSessionsAsync(cancellationToken);
            await RunAsync("Spike-before", 60, stableRate);
            await RunAsync("Spike-peak", 30, 10000);
            await RunAsync("Spike-recovery", 90, stableRate);
            await environment.RenewSessionsAsync(cancellationToken);
            await evidence.CaptureDatabaseAsync(Path.Combine(output, "soak-before.json"), cancellationToken);

            // Six contiguous ten-minute segments retain one API/container/cache/pool throughout the soak.
            // Only fixture credentials are renewed between segments; the process is never restarted.
            for (var segment = 1; segment <= 6; segment++)
            {
                await RunAsync($"Soak-{segment}", 600, stableRate);
                await environment.RenewSessionsAsync(cancellationToken);
            }

            // An idle recovery window distinguishes retained application data/GC capacity from stuck connections.
            await Task.Delay(TimeSpan.FromSeconds(smoke ? 1 : 30), cancellationToken);
            await evidence.CaptureDatabaseAsync(Path.Combine(output, "soak-after.json"), cancellationToken);
            await evidence.CapturePlansAsync(output, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(output, "completion.json"), JsonSerializer.Serialize(new
            {
                CompletedUtc = DateTimeOffset.UtcNow,
                SmokeOnly = smoke,
                StableRateSelected = stableRate,
                AllRequiredTypesExecuted = !smoke,
                PhaseCount = results.Count,
                TargetProven = !smoke && results.Any(value => value.Name == "Load" && value.Stable &&
                    value.SuccessfulRps >= 9000),
                MaxStableMeasuredRps = results.Where(value => value.Stable &&
                    (value.Name == "Load" || value.Name.StartsWith("Stress-", StringComparison.Ordinal)))
                    .Select(value => (double?)value.SuccessfulRps).Max()
            }), cancellationToken);
            logger.LogInformation("Baseline evidence saved; completion does not imply the capacity target passed");
        }
        finally
        {
            await sampling.CancelAsync();
            await sampler;
        }
    }
}

internal sealed class PerformanceBaselineFactAttribute : FactAttribute
{
    public PerformanceBaselineFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BROUJ_RUN_PERFORMANCE_BASELINE") != "1")
        {
            Skip = "Explicit opt-in required: use scripts/performance/Run-LocalBaseline.ps1 (about 90 minutes).";
        }
    }
}
