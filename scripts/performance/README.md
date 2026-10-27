# Part 11 local performance baseline

Run from the repository root, with .NET 10, Node.js and Docker Desktop's Linux engine available:

```powershell
powershell -NoProfile -File scripts/performance/Run-LocalBaseline.ps1 -Smoke
powershell -NoProfile -File scripts/performance/Run-LocalBaseline.ps1
node scripts/performance/Summarize-Baseline.mjs artifacts/performance/<UTC-run-directory>
```

The full run takes approximately 86 minutes plus container startup, seeding, credential renewal and
overload draining. Smoke uses two-second phases and is explicitly marked as smoke evidence. It cannot
satisfy the Part 11 duration requirements. The opt-in test is skipped by ordinary solution test runs.
Build Release binaries before timing; the script does this automatically. Do not run builds, other SQL
tests, benchmark jobs or profiling sessions on this machine during a measured run.

## Isolation and security

The existing Infrastructure integration-test project owns a SQL Server Testcontainer and launches the
real API Release DLL as a separate process. The API runs in Production, accepts only localhost and
uses HTTPS with a temporary certificate pinned by the generator. No developer user-secrets or existing
application database is read. Bootstrap provisioning is explicitly disabled. The container is disposed
and the generated PFX is deleted at the end of the test.

JWT signature, issuer, audience, normal 15-minute expiry, current-account and role checks, ownership,
CSRF, validation, audit, transaction, request limits and rate limiting remain enabled. The fixture uses
the existing JWT service to issue credentials for its seeded actors. Login/password-hashing throughput
is outside this workload. Real CSRF tokens/cookies are obtained over HTTPS and fixture credentials are
renewed between phases and after each ten-minute soak segment. No token, cookie, password, hash or
signing secret is written to results.

Required rate budgets are synthetic configuration: public reads 20,000/s, authentication 1,000/s,
reservation writes 1,000/s, admin writes 100/s and uploads 10/s. Every category uses the existing
per-IP, per-instance, one-second fixed-window limiter. There are no trusted proxies and the generator
has one source IP. These are documented benchmark budgets, not deployment recommendations. Rejection
responses remain measured and cannot be counted as successful throughput.

Information-level application/request logging remains active. The runner drains host output and
persists only the fixed-marker diagnostic JSON; it does not store arbitrary host exception messages.
`ILogger<T>` records phase progress and lifecycle events. Diagnostics are disabled in ordinary API
startup; enable them explicitly with `PerformanceDiagnostics__Enabled=true` to use the same sampler.

## Dataset and traffic

The disposable database uses the unchanged EF model and approved indexes. Synthetic seed inserts happen
before timing using parameterized, fixed SQL; they omit application audit history for the initial data.
Every timed reservation goes through the actual HTTP/application transaction path. The existing
staff-only audit interceptor remains registered; customer writes do not create staff audit rows.

| Resource | Initial count |
|---|---:|
| Cities / areas | 10 / 100 |
| Regular projects / units | 1,000 / 100,000 |
| Customers / active customer sessions / staff | 10,000 / 128 / 1 |
| Cancelled reservation history | 20,000 |
| Project amenities / image metadata | 1,000 / 1,000 |
| Unit amenities / image metadata | 100,000 / 100,000 |
| Current terms | One active version, approximately 4 KiB content |

Mixed traffic repeats the approved 80/10/5/5 percentage weights every 100 offered operations: public
reads, customer reads, admin reads and reservation writes. Public reads rotate evenly across seven
scenarios over 700 operations. The actual completed mix is recorded per scenario; it can diverge from
offered weights under overload when the bounded generator drops work.

Public reads are cities, areas, project list/details, unit list/details and current terms. Customer
reads list the current customer's reservations; admin reads list reservations with the real Admin
policy. Every scenario also runs separately. Lists use 20-item pages, including late pages. Project
and unit identities rotate through the catalogue; details include real child image/amenity projections.
Image bytes, initiative projects, uploads, orders, time plans, login/refresh endpoints and browser HTML
are outside this specific workload. Reservation writes consume distinct (customer, unit) pairs, walking
the catalogue for each customer before moving to the next one. This intentionally documented fixture
distribution is not an assertion about deployed customer behavior. Reservation history grows throughout
the test and is not deleted between phases. No staff mutation is part of this workload, so staff audit
history does not grow.

Memory caching stays registered. No current feature handler calls the cache adapter, so zero cache
calls and an undefined hit ratio are expected; SQL Server's own buffer/plan caches still warm naturally.

## Schedule and measurements

1. Warm up at 25 RPS for 30 seconds.
2. Calibrate 25, 50, 100, 200 and 400 RPS for 20 seconds each. Select the highest passing candidate;
   fall back to 10 RPS if none passes, without calling that fallback proven stable.
3. Run each of ten scenarios separately for 30 seconds at `min(50, selected rate)` RPS.
4. Run mixed load for five minutes at the selected rate.
5. Stress mixed traffic at 500, 1,000, 2,000, 4,000, 8,000 and 10,000 offered RPS, 100 seconds each.
6. Spike: 60 seconds at the selected rate, 30 seconds at 10,000 RPS, 90 seconds recovery at the selected rate.
7. Soak for six consecutive ten-minute segments at the selected rate, retaining the same API, SQL
   container, database, cache and connection pool. Short CSRF/JWT renewal gaps between segments are
   documented in actual wall duration. No process restarts or forced GC occurs.
8. Allow 30 seconds idle recovery, then capture database state and last actual execution plans.

The open-arrival generator schedules every 20 ms using elapsed wall time, with 256 workers and at most
256 queued jobs. When its queue is full, work is dropped and counted explicitly. Latency includes
scheduling, queueing, TLS/HTTP, server execution and full response-body consumption. Histograms merge
integer millisecond buckets before computing nearest-rank p50/p95/p99; they never average percentiles.
The final bucket is overflow at 60,001 ms. Each HTTP request has a five-second timeout. Status zero is
a network/timeout failure; every unexpected HTTP status, including a reservation conflict, is an error.

Successful RPS is expected-status responses divided by actual phase wall time, including draining.
Completed RPS also includes failed/rejected responses. Rejected 429s are separate from unexpected
errors. A stable phase requires p95 <= 200 ms, p99 <= 500 ms, unexpected errors < 1%, zero dropped
operations, zero rejects and at least 99% of offered RPS completed successfully. A short calibration
pass alone is not a sustained capacity result. No aggregate system-capacity claim is derived from a
single endpoint.

Every ten seconds the API records process CPU (normalized by its logical processors), working set,
private and managed memory, allocation/GC/heap/pause counters, ThreadPool size/queue/completions, EF
command latency histograms, SqlClient pool counters and cache hits/misses. EF command timing measures
provider command execution; materialization and response serialization are included in HTTP latency.
SQL and generator sampling separately records SQL sessions/running/blocked requests, selected wait
statistics, allocated database files, Docker CPU/memory/I/O and generator CPU/queue. Docker CPU percent
uses the Docker convention where one saturated CPU is 100%; API/generator percentages normalize by
their own processor count. Resource samples are not a continuous profiler and can miss brief peaks.

SQL waits are cumulative within the isolated container; use differences between samples and keep
setup/seed waits separate from timed work. Pool counters are process-local. A pool retaining idle
physical connections is not itself a connection leak. Soak review compares heap/working-set trends,
GC activity, active/free/reclaimed connections, idle recovery and outstanding transactions. The summary
reports trends without automatically claiming a finite test proves the absence of every possible leak.

`LAST_QUERY_PLAN_STATS` is enabled only in the disposable database. The runner obtains up to 30 hot
read plans using `sys.dm_exec_query_plan_stats`, recording whether runtime counters actually exist.
The plan inventory includes execution count, aggregate SQL elapsed/CPU/logical reads, operators and
spill/index suggestions. These are last-execution plans plus cumulative query statistics, not a trace
of every execution. Inspect the `.sqlplan` files before recommending an index; this Part changes no schema.

## Evidence

Each UTC run directory under ignored `artifacts/performance/` contains environment metadata, phase
results, raw resource JSONL, soak database snapshots and SQL plans. The summary script writes
`summary.json`, `phases.csv` and a SHA-256 artifact manifest. Resource-window percentile maxima are
explicitly labeled; a percentile of all database commands is not reconstructed from window percentiles.
Retain the raw run directory alongside the final report. An execution test passing means the scenario
schedule completed; capacity and SLO acceptance are independent fields in the evidence.

See the final [Part 11 report](../../docs/Brouj.Docs/Part11.PerformanceVerification.md) for the measured
outcome, bottleneck evidence, limitations, tests and Definition of Done status.
