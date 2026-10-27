using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Brouj.Infrastructure.IntegrationTests.Performance;

/// <summary>Collects aggregate SQL/host pressure and last actual plans from the disposable database only.</summary>
internal sealed class SqlEvidence(BaselineEnvironment environment, ILogger<SqlEvidence> logger)
{
    public async Task SampleAsync(string output, CancellationToken cancellationToken)
    {
        await using var file = new StreamWriter(Path.Combine(output, "generator-sql-resources.jsonl"));
        using var generator = Process.GetCurrentProcess();
        var previousCpu = generator.TotalProcessorTime;
        var clock = Stopwatch.StartNew();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await using var connection = await environment.OpenDatabaseAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandTimeout = 5;
                command.CommandText = """
                    SELECT COUNT(*) AS Sessions,
                        COALESCE(SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END), 0) AS Running
                    FROM sys.dm_exec_sessions WHERE database_id = DB_ID() AND session_id <> @@SPID;
                    SELECT COUNT(*) AS Requests,
                        COALESCE(SUM(CASE WHEN blocking_session_id > 0 THEN 1 ELSE 0 END), 0) AS Blocked,
                        COALESCE(MAX(wait_time), 0) AS MaxWaitMs
                    FROM sys.dm_exec_requests WHERE database_id = DB_ID() AND session_id <> @@SPID;
                    SELECT SUM(size) * 8192.0 AS FileBytes FROM sys.database_files;
                    SELECT wait_type, waiting_tasks_count, wait_time_ms FROM sys.dm_os_wait_stats
                    WHERE wait_type IN ('THREADPOOL', 'LCK_M_U', 'LCK_M_X', 'LCK_M_S', 'WRITELOG',
                        'PAGEIOLATCH_SH', 'SOS_SCHEDULER_YIELD', 'RESOURCE_SEMAPHORE', 'ASYNC_NETWORK_IO');
                    """;
                var tables = await ReadRowsAsync(command, cancellationToken);
                generator.Refresh();
                var cpu = generator.TotalProcessorTime;
                var sample = new
                {
                    Utc = DateTimeOffset.UtcNow,
                    GeneratorCpuPercent = (cpu - previousCpu).TotalSeconds / clock.Elapsed.TotalSeconds /
                        Environment.ProcessorCount * 100,
                    GeneratorWorkingSetBytes = generator.WorkingSet64,
                    GeneratorThreadPoolQueue = ThreadPool.PendingWorkItemCount,
                    Sql = tables,
                    Docker = await DockerStatsAsync(cancellationToken)
                };
                await file.WriteLineAsync(JsonSerializer.Serialize(sample).AsMemory(), cancellationToken);
                await file.FlushAsync(cancellationToken);
                previousCpu = cpu;
                clock.Restart();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("SQL and generator sampling stopped");
        }
    }

    public async Task CapturePlansAsync(string output, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(output, "plans");
        Directory.CreateDirectory(directory);
        await using var connection = await environment.OpenDatabaseAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 60;
        // Select only read batches of the benchmark database. Credential/session batches are excluded.
        command.CommandText = """
            SELECT TOP (30) qs.execution_count, qs.total_elapsed_time, qs.total_worker_time,
                qs.total_logical_reads, qs.total_logical_writes, qs.last_elapsed_time,
                SUBSTRING(st.text, qs.statement_start_offset / 2 + 1,
                    (CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(st.text)
                    ELSE qs.statement_end_offset END - qs.statement_start_offset) / 2 + 1) AS Statement,
                CONVERT(nvarchar(max), actual.query_plan) AS ActualPlan
            FROM sys.dm_exec_query_stats qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
            CROSS APPLY sys.dm_exec_plan_attributes(qs.plan_handle) attr
            OUTER APPLY sys.dm_exec_query_plan_stats(qs.plan_handle) actual
            WHERE attr.attribute = 'dbid' AND CONVERT(int, attr.value) = DB_ID()
                AND st.text LIKE '%SELECT%' AND st.text NOT LIKE '%INSERT %'
                AND st.text NOT LIKE '%UPDATE %' AND st.text NOT LIKE '%DELETE %'
                AND st.text NOT LIKE '%HashPassword%' AND st.text NOT LIKE '%RefreshToken%'
                AND st.text NOT LIKE '%IdentityNumber%' AND st.text NOT LIKE '%dm_exec%'
                AND st.text NOT LIKE '%dm_os%' AND st.text NOT LIKE '%database_files%'
            ORDER BY qs.total_elapsed_time DESC;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var inventory = new List<object>();
        var index = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            index++;
            var plan = reader.IsDBNull(7) ? null : reader.GetString(7);
            var file = $"hot-{index:D2}.sqlplan";

            if (plan is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(directory, file), plan, cancellationToken);
            }

            inventory.Add(new
            {
                Executions = reader.GetInt64(0), TotalElapsedMicroseconds = reader.GetInt64(1),
                TotalCpuMicroseconds = reader.GetInt64(2), TotalLogicalReads = reader.GetInt64(3),
                TotalLogicalWrites = reader.GetInt64(4), LastElapsedMicroseconds = reader.GetInt64(5),
                Statement = reader.GetString(6), ActualPlan = plan is null ? null : file,
                HasRuntimeCounters = plan?.Contains("RunTimeCountersPerThread", StringComparison.Ordinal) == true
            });
        }

        await File.WriteAllTextAsync(Path.Combine(directory, "inventory.json"),
            JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
        logger.LogInformation("Captured {Count} hot-query records with last actual SQL plans where available", index);
    }

    public async Task CaptureDatabaseAsync(string path, CancellationToken cancellationToken)
    {
        await using var connection = await environment.OpenDatabaseAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT_BIG(*) AS Reservations FROM Reservations;
            SELECT COUNT_BIG(*) AS ActivityLogs FROM ActivityLogs;
            SELECT COUNT(*) AS OpenTransactions FROM sys.dm_tran_session_transactions t
            JOIN sys.dm_exec_sessions s ON t.session_id = s.session_id WHERE s.database_id = DB_ID();
            SELECT name, size * 8192.0 AS SizeBytes, FILEPROPERTY(name, 'SpaceUsed') * 8192.0 AS UsedBytes
            FROM sys.database_files;
            """;
        var rows = await ReadRowsAsync(command, cancellationToken);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { Utc = DateTimeOffset.UtcNow, Rows = rows }),
            cancellationToken);
    }

    private async Task<JsonElement?> DockerStatsAsync(CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("docker")
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var argument in new[] { "stats", "--no-stream", "--format", "{{json .}}", environment.ContainerId })
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var text = await stdout;
        await stderr;

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        using var document = JsonDocument.Parse(text);

        return document.RootElement.Clone();
    }

    private static async Task<List<List<Dictionary<string, object?>>>> ReadRowsAsync(SqlCommand command,
        CancellationToken cancellationToken)
    {
        var tables = new List<List<Dictionary<string, object?>>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        do
        {
            var rows = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                rows.Add(row);
            }

            tables.Add(rows);
        }
        while (await reader.NextResultAsync(cancellationToken));

        return tables;
    }
}
