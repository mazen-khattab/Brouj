import { readFile, writeFile, readdir } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';

// Consume only the runner's sanitized artifacts. No database, credentials or network access is needed.
const directory = resolve(process.argv[2] ?? '');
if (!process.argv[2]) {
  throw new Error('Usage: node scripts/performance/Summarize-Baseline.mjs <artifact-directory>');
}

const json = async name => JSON.parse(await readFile(join(directory, name), 'utf8'));
const lines = async name => (await readFile(join(directory, name), 'utf8')).trim().split(/\r?\n/)
  .filter(Boolean).map(line => JSON.parse(line));
const phases = await json('phases.json');
const environment = await json('environment.json');
const completion = await json('completion.json');
const api = await lines('api-resources.jsonl');
const sql = await lines('generator-sql-resources.jsonl');
const plans = await json('plans/inventory.json');

// Reject incomplete/corrupt evidence before producing a report that could be mistaken for a full run.
for (const phase of phases) {
  if (phase.Total.Count + phase.Dropped !== phase.Offered ||
      phase.Total.Successful + phase.Total.UnexpectedErrors + phase.Total.RateLimitRejects !== phase.Total.Count ||
      phase.Scenarios.reduce((sum, scenario) => sum + scenario.Count, 0) !== phase.Total.Count) {
    throw new Error(`Inconsistent request accounting in ${phase.Name}`);
  }
}
if (!api.length || !sql.length || !plans.some(plan => plan.HasRuntimeCounters)) {
  throw new Error('Missing API/SQL resource samples or last actual SQL plans. Do not claim the review is complete.');
}
if (!completion.SmokeOnly) {
  const duration = prefix => phases.filter(phase => phase.Name.startsWith(prefix))
    .reduce((sum, phase) => sum + phase.ScheduledSeconds, 0);
  if (duration('Load') !== 300 || duration('Stress-') !== 600 || duration('Spike-') !== 180 ||
      duration('Soak-') !== 3600 || duration('Separate-') !== 300) {
    throw new Error('The approved performance schedule was not completed.');
  }
}
const mean = values => values.length ? values.reduce((a, b) => a + b, 0) / values.length : null;
const max = values => values.length ? Math.max(...values) : null;
const median = values => values.length ? values.toSorted((a, b) => a - b)[Math.floor(values.length / 2)] : null;
const between = (samples, start, finish) => samples.filter(sample => Date.parse(sample.Utc) >= Date.parse(start)
  && Date.parse(sample.Utc) <= Date.parse(finish));
const mib = value => value == null ? null : value / 1048576;
const range = values => ({ First: values.at(0) ?? null, Last: values.at(-1) ?? null, Maximum: max(values) });
const connectionValues = (samples, name) => samples.map(sample => sample.Connections[name]).filter(Number.isFinite);
function waitDeltas(samples) {
  if (samples.length < 2) {
    return null;
  }
  const initial = new Map(samples[0].Sql[3].map(row => [row.wait_type, row]));
  return samples.at(-1).Sql[3].map(row => ({
    Wait: row.wait_type,
    Count: row.waiting_tasks_count - (initial.get(row.wait_type)?.waiting_tasks_count ?? 0),
    Milliseconds: row.wait_time_ms - (initial.get(row.wait_type)?.wait_time_ms ?? 0),
  }));
}

const resourceRows = phases.map(phase => {
  const samples = between(api, phase.StartedUtc, phase.FinishedUtc);
  const server = between(sql, phase.StartedUtc, phase.FinishedUtc);
  return {
    Phase: phase.Name,
    Samples: samples.length,
    CpuPercentMean: mean(samples.map(value => value.CpuPercent)),
    CpuPercentMax: max(samples.map(value => value.CpuPercent)),
    WorkingSetMiB: range(samples.map(value => mib(value.WorkingSetBytes))),
    PrivateMiB: range(samples.map(value => mib(value.PrivateBytes))),
    ManagedMiB: range(samples.map(value => mib(value.ManagedBytes))),
    HeapMiB: range(samples.map(value => mib(value.HeapBytes))),
    Gen0Delta: samples.length ? samples.at(-1).Gen0 - samples[0].Gen0 : null,
    Gen1Delta: samples.length ? samples.at(-1).Gen1 - samples[0].Gen1 : null,
    Gen2Delta: samples.length ? samples.at(-1).Gen2 - samples[0].Gen2 : null,
    AllocatedMiBDelta: samples.length ? mib(samples.at(-1).TotalAllocatedBytes - samples[0].TotalAllocatedBytes) : null,
    GcPausePercentMax: max(samples.map(value => value.GcPausePercent)),
    ThreadPoolThreadsMax: max(samples.map(value => value.ThreadPoolThreads)),
    ThreadPoolQueueMax: max(samples.map(value => value.ThreadPoolQueue)),
    DatabaseCommands: samples.reduce((sum, value) => sum + value.Database.Count, 0),
    DatabaseP50WindowMaxMs: max(samples.map(value => value.Database.P50Ms).filter(Number.isFinite)),
    DatabaseP95WindowMaxMs: max(samples.map(value => value.Database.P95Ms).filter(Number.isFinite)),
    DatabaseP99WindowMaxMs: max(samples.map(value => value.Database.P99Ms).filter(Number.isFinite)),
    ActiveConnectionsMax: max(connectionValues(samples, 'number-of-active-connections')),
    PooledConnectionsMax: max(connectionValues(samples, 'number-of-pooled-connections')),
    FreeConnectionsMax: max(connectionValues(samples, 'number-of-free-connections')),
    ReclaimedConnectionsMax: max(connectionValues(samples, 'number-of-reclaimed-connections')),
    GeneratorCpuPercentMax: max(server.map(value => value.GeneratorCpuPercent)),
    GeneratorThreadPoolQueueMax: max(server.map(value => value.GeneratorThreadPoolQueue)),
    DockerCpuPercentMax: max(server.filter(value => value.Docker)
      .map(value => Number.parseFloat(value.Docker.CPUPerc))),
    SqlBlockedRequestsMax: max(server.map(value => value.Sql[1][0].Blocked)),
    SqlWaitMsMax: max(server.map(value => value.Sql[1][0].MaxWaitMs)),
    SqlWaitDeltas: waitDeltas(server),
  };
});

const soakPhases = phases.filter(phase => phase.Name.startsWith('Soak-'));
const soak = between(api, soakPhases[0].StartedUtc, soakPhases.at(-1).FinishedUtc);
const firstWindow = soak.filter(value => Date.parse(value.Utc) < Date.parse(soak[0].Utc) + 300000);
const lastWindow = soak.filter(value => Date.parse(value.Utc) > Date.parse(soak.at(-1).Utc) - 300000);

// Regression is evidence of a trend, not an automatic declaration that a leak exists or is absent.
function slopePerMinute(samples, field) {
  const pairs = samples.map(value => [(Date.parse(value.Utc) - Date.parse(samples[0].Utc)) / 60000, value[field]]);
  const x = mean(pairs.map(value => value[0]));
  const y = mean(pairs.map(value => value[1]));
  const denominator = pairs.reduce((sum, value) => sum + (value[0] - x) ** 2, 0);
  return denominator ? pairs.reduce((sum, value) => sum + (value[0] - x) * (value[1] - y), 0) / denominator : null;
}

const soakEvidence = {
  DurationSeconds: soakPhases.reduce((sum, phase) => sum + phase.ScheduledSeconds, 0),
  WallSeconds: (Date.parse(soakPhases.at(-1).FinishedUtc) - Date.parse(soakPhases[0].StartedUtc)) / 1000,
  Completed: soakPhases.reduce((sum, phase) => sum + phase.Total.Count, 0),
  Successful: soakPhases.reduce((sum, phase) => sum + phase.Total.Successful, 0),
  Unexpected: soakPhases.reduce((sum, phase) => sum + phase.Total.UnexpectedErrors, 0),
  Rejected: soakPhases.reduce((sum, phase) => sum + phase.Total.RateLimitRejects, 0),
  Dropped: soakPhases.reduce((sum, phase) => sum + phase.Dropped, 0),
  AllSegmentsStable: soakPhases.every(phase => phase.Stable),
  ResourceSamples: soak.length,
  FirstFiveMinuteMedianWorkingSetMiB: median(firstWindow.map(value => mib(value.WorkingSetBytes))),
  LastFiveMinuteMedianWorkingSetMiB: median(lastWindow.map(value => mib(value.WorkingSetBytes))),
  FirstFiveMinuteMedianManagedMiB: median(firstWindow.map(value => mib(value.ManagedBytes))),
  LastFiveMinuteMedianManagedMiB: median(lastWindow.map(value => mib(value.ManagedBytes))),
  FirstFiveMinuteMinimumHeapMiB: Math.min(...firstWindow.map(value => mib(value.HeapBytes))),
  LastFiveMinuteMinimumHeapMiB: Math.min(...lastWindow.map(value => mib(value.HeapBytes))),
  ManagedMiBSlopePerMinute: mib(slopePerMinute(soak, 'ManagedBytes')),
  WorkingSetMiBSlopePerMinute: mib(slopePerMinute(soak, 'WorkingSetBytes')),
  Gen2Collections: soak.length ? soak.at(-1).Gen2 - soak[0].Gen2 : null,
  ActiveConnections: range(connectionValues(soak, 'number-of-active-connections')),
  PooledConnections: range(connectionValues(soak, 'number-of-pooled-connections')),
  ReclaimedConnections: range(connectionValues(soak, 'number-of-reclaimed-connections')),
  FinalIdleSample: api.at(-1),
  DatabaseBefore: await json('soak-before.json'),
  DatabaseAfter: await json('soak-after.json'),
  SqlWaitDeltas: waitDeltas(between(sql, soakPhases[0].StartedUtc, soakPhases.at(-1).FinishedUtc)),
};

const planReview = [];
for (const [index, record] of plans.entries()) {
  const xml = record.ActualPlan ? await readFile(join(directory, 'plans', record.ActualPlan), 'utf8') : '';
  const physicalOps = [...xml.matchAll(/PhysicalOp="([^"]+)"/g)].map(value => value[1]);
  const runtimeRows = [...xml.matchAll(/ActualRows="([^"]+)"/g)].map(value => Number(value[1]));
  const objects = [...new Set([...xml.matchAll(/<Object\b[^>]*Table="([^"]+)"[^>]*>/g)].map(value => value[1]))];
  planReview.push({
    Rank: index + 1,
    Executions: record.Executions,
    MeanElapsedMs: record.TotalElapsedMicroseconds / record.Executions / 1000,
    MeanCpuMs: record.TotalCpuMicroseconds / record.Executions / 1000,
    MeanLogicalReads: record.TotalLogicalReads / record.Executions,
    TotalElapsedSeconds: record.TotalElapsedMicroseconds / 1e6,
    ActualPlan: record.ActualPlan,
    HasRuntimeCounters: record.HasRuntimeCounters,
    PhysicalOperators: Object.fromEntries([...new Set(physicalOps)].map(op => [op, physicalOps.filter(x => x === op).length])),
    Objects: objects,
    MaximumOperatorActualRows: max(runtimeRows),
    HasSpillWarning: /SpillToTempDb|SpillOccurred|HashSpillDetails|SortSpillDetails/.test(xml),
    HasMissingIndexSuggestion: xml.includes('<MissingIndex'),
    Statement: record.Statement,
  });
}

const summary = {
  Environment: environment, Completion: completion,
  TotalOffered: phases.reduce((sum, phase) => sum + phase.Offered, 0),
  TotalCompleted: phases.reduce((sum, phase) => sum + phase.Total.Count, 0),
  TotalSuccessful: phases.reduce((sum, phase) => sum + phase.Total.Successful, 0),
  TotalUnexpected: phases.reduce((sum, phase) => sum + phase.Total.UnexpectedErrors, 0),
  TotalRejects: phases.reduce((sum, phase) => sum + phase.Total.RateLimitRejects, 0),
  TotalDropped: phases.reduce((sum, phase) => sum + phase.Dropped, 0),
  CacheHits: api.at(-1)?.CacheHits ?? null, CacheMisses: api.at(-1)?.CacheMisses ?? null,
  ApiSamples: api.length, SqlGeneratorSamples: sql.length,
  MaximumApiSampleGapSeconds: max(api.slice(1).map((value, index) =>
    (Date.parse(value.Utc) - Date.parse(api[index].Utc)) / 1000)),
  ResourceRows: resourceRows, SoakEvidence: soakEvidence, PlanReview: planReview,
};
await writeFile(join(directory, 'summary.json'), JSON.stringify(summary, null, 2));
const quote = value => `"${String(value ?? '').replaceAll('"', '""')}"`;
const phaseRows = phases.map(phase => ({
  Phase: phase.Name, Seconds: phase.ScheduledSeconds, ActualSeconds: phase.ActualSeconds,
  OfferedRps: phase.OfferedRps, SuccessfulRps: phase.SuccessfulRps, CompletedRps: phase.CompletedRps,
  P50Ms: phase.Total.P50Ms, P95Ms: phase.Total.P95Ms, P99Ms: phase.Total.P99Ms,
  UnexpectedErrorPercent: phase.UnexpectedErrorRate * 100, Rejected: phase.Total.RateLimitRejects,
  Offered: phase.Offered, Completed: phase.Total.Count, Dropped: phase.Dropped,
  PeakConcurrency: phase.PeakConcurrency, MaximumSchedulerDelayMs: phase.MaximumSchedulerDelayMs,
  MeanPayloadBytes: phase.Total.MeanPayloadBytes, MaximumPayloadBytes: phase.Total.MaximumPayloadBytes,
  Stable: phase.Stable,
}));
const columns = Object.keys(phaseRows[0]);
await writeFile(join(directory, 'phases.csv'), [columns.map(quote).join(','), ...phaseRows.map(row =>
  columns.map(column => quote(row[column])).join(','))].join('\n') + '\n');

const manifest = [];
for (const name of ['environment.json', 'completion.json', 'phases.json', 'api-resources.jsonl',
  'generator-sql-resources.jsonl', 'soak-before.json', 'soak-after.json', 'summary.json', 'phases.csv',
  ...((await readdir(join(directory, 'plans'))).map(name => `plans/${name}`))]) {
  const bytes = await readFile(join(directory, name));
  manifest.push({ File: name, Bytes: bytes.length, Sha256: createHash('sha256').update(bytes).digest('hex') });
}
await writeFile(join(directory, 'manifest.json'), JSON.stringify(manifest, null, 2));
console.info(JSON.stringify({ Directory: directory, SmokeOnly: completion.SmokeOnly,
  TargetProven: completion.TargetProven, MaxStableMeasuredRps: completion.MaxStableMeasuredRps,
  Completed: summary.TotalCompleted, SoakSeconds: soakEvidence.DurationSeconds,
  PlansWithRuntimeCounters: plans.filter(plan => plan.HasRuntimeCounters).length }, null, 2));
