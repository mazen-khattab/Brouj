# Part 11 — Performance verification

Date: 2026-10-27. Run: `20261027T165154Z`. **The 9,000–10,000 RPS target was not achieved.**
All four approved test types and all ten separate scenarios were executed against the real Production
API pipeline and an isolated SQL Server Testcontainer. This is the owner-approved local baseline.

The highest measured passing sustained point was **399.85 successful RPS for five minutes**, with
**p50 33 ms, p95 136 ms, p99 265 ms**, zero errors, zero rejects and zero drops. **400 offered RPS
did not remain stable during the hour-long soak**: throughput deteriorated, requests timed out and
operations were dropped. No hour-long stable rate was established. The earlier five-minute result is
not a production capacity guarantee.

The main measured bottleneck is SQL read work, especially the exact, unfiltered admin reservation
count and full-history paging/sorting, with CPU pressure and a saturated connection pool. Schema,
pagination semantics, cache policy and security controls were not changed to improve the result.

## Evidence and reproduction

The complete sanitized evidence is checked in under [Part11.Evidence](Part11.Evidence/manifest.json):

- [Environment](Part11.Evidence/environment.json), [completion](Part11.Evidence/completion.json),
  [phase CSV](Part11.Evidence/phases.csv) and [per-scenario phase JSON](Part11.Evidence/phases.json).
- [Resource/SQL/soak summary](Part11.Evidence/summary.json),
  [raw API samples](Part11.Evidence/api-resources.jsonl) and
  [raw generator/SQL/Docker samples](Part11.Evidence/generator-sql-resources.jsonl).
- [Last actual plan inventory](Part11.Evidence/plans/inventory.json), thirty `.sqlplan` files,
  [before-soak database state](Part11.Evidence/soak-before.json) and
  [after-soak database state](Part11.Evidence/soak-after.json).

There are 517 API samples and 517 generator/SQL samples; the maximum interval between API samples was
10.09 seconds. The SHA-256 manifest covers the raw evidence and generated summaries. The original run
also remains under ignored `artifacts/performance/20261027T165154Z/`. No credentials, cookies, JWTs,
password hashes or private certificate are included in this evidence. Plan parameters refer only to
the disposable synthetic dataset.

Reproduction commands and the complete schedule are in the [benchmark runbook](../../scripts/performance/README.md).
The API binary measured was SHA-256
`B3D8888D4EDAC39B6D013B9D467C0195CCFFF9B41FD3CFA37DEDA3E54A2F6FF6`, built in Release from the working tree
based on commit `e3053f557b47f9a9750b36d24815d92a9d4bd7a6` plus these Part 11 changes.
The earlier two-second-per-phase smoke run `20261027T164842Z` passed execution validation but is not
used as duration or capacity evidence.

## Documented environment and workload

| Dimension | Configuration |
|---|---|
| API instances | One separate Release Kestrel process, Production environment, HTTPS on loopback |
| Host | Windows 10.0.26200, x64, eight logical processors; AMD64 Family 23 Model 24 Stepping 1 |
| Host memory | 14,925,369,344 bytes reported available to the runtime, approximately 13.9 GiB |
| Runtime | .NET 10.0.12; SDK 10.0.401 |
| GC configuration | API runtimeconfig enables server GC; `environment.json`'s `ServerGc=false` describes the generator/test host |
| Database | SQL Server 2022 Developer, 16.0.4305.5, Ubuntu 22.04.5, Docker Desktop Linux engine |
| SQL tier | Local container, no dedicated CPU reservation; Docker VM has eight CPUs and approximately 6.72 GiB memory |
| Image | `mcr.microsoft.com/mssql/server:2022-latest`; exact SQL build recorded, image tag is mutable |
| Pool | API SqlClient maximum 100 connections; collector has a separate process-local pool |
| Cache | Existing DistributedMemoryCache; no measured feature handler calls the adapter |
| Network | Generator → local HTTPS API → Docker Desktop mapped SQL TCP port; all share one physical host |
| Data | 10 cities, 100 areas, 1,000 regular projects, 100,000 units, 10,000 customers, 128 active fixture customer sessions, one Admin |
| Children/history | One amenity and image metadata per project/unit; 20,000 initial cancelled reservations; one active terms version |
| Mixed weights | 80% public reads, 10% customer reads, 5% admin reads, 5% reservation creation: 95:5 reads/writes |
| Pagination | 20 items; rotating catalogue identities and early/late list pages |
| Generator | Open arrivals, 20 ms scheduling tick, 256 workers, 256 queued operations, five-second HTTP timeout |
| Security | Production JWT/current-actor/role/ownership checks, real CSRF proof/cookies, validation, transactions, request limits, rate limiting and normal information logging remain active |
| Synthetic rate budgets | Per-IP one-second windows: public 20,000; authentication 1,000; reservations 1,000; admin writes 100; uploads 10 |

Fixture credentials use the existing JWT issuer and normal fifteen-minute lifetime; real CSRF bootstrap
runs over HTTPS. Credential renewal occurs before applicable phases and between ten-minute soak
segments. This measures authenticated requests, not login/password hashing or refresh throughput.
The temporary SQL administrator credential is isolated to the test container; production least-privilege
SQL permissions are not certified by this run.

The dataset includes regular projects and metadata reads; it does not exercise initiative projects,
orders, uploads, image downloads, time plans or external I/O. Reservation writes consume unique
customer/unit pairs, walking all units for one customer before using the next customer. That fixture
distribution is documented rather than asserted to match actual customer behavior. Staff-only audit
rules remain enabled; customer writes do not generate staff audit rows.

Acceptance was p95 ≤ 200 ms, p99 ≤ 500 ms and unexpected errors < 1%. A reported stable point additionally
requires no drops/rejects and at least 99% of offered RPS completed successfully. Latency includes
generator scheduling/queue time through complete response consumption, using merged 1 ms histograms.
Successful RPS uses actual phase duration including draining. Status zero denotes transport/timeout
failure; 429 is recorded separately. Percentiles are never averaged across phases.

## Separate scenario measurements

Each scenario ran for thirty seconds at 50 offered RPS after warmup/calibration. All completed with
zero unexpected errors, zero rate-limit rejects and zero drops. Payload figures are JSON body bytes,
excluding headers and TLS overhead; reservation creation request bodies contain only a unit GUID.

| Scenario | Successful RPS | p50 ms | p95 ms | p99 ms | Mean/max response bytes |
|---|---:|---:|---:|---:|---:|
| Cities | 49.96 | 35 | 42 | 46 | 731 / 731 |
| Areas | 49.98 | 36 | 43 | 44 | 2,731 / 2,733 |
| Project list | 49.97 | 37 | 59 | 61 | 4,870 / 4,872 |
| Project details | 49.97 | 36 | 44 | 46 | 900 / 901 |
| Unit list | 49.98 | 35 | 59 | 60 | 5,909 / 5,910 |
| Unit details | 49.97 | 38 | 45 | 48 | 500 / 500 |
| Current terms | 49.98 | 33 | 40 | 41 | 4,199 / 4,199 |
| Customer reads | 49.97 | 37 | 45 | 51 | 669 / 5,805 |
| Reservation writes | 49.96 | 52 | 61 | 68 | 77 / 77 |
| Admin reads | 49.93 | 69 | 88 | 97 | 5,811 / 5,816 |

These endpoint measurements establish behavior at fifty RPS, not endpoint maximum capacities.

## Load, stress, spike and soak results

Warmup lasted thirty seconds. Five twenty-second calibration steps (25–400 RPS) selected 400 RPS.
The five-minute load completed all 120,000 operations: 114,000 reads and 6,000 successful creations,
with the exact approved 80/10/5/5 category mix. Mean/max response payload was 2,629/5,910 bytes.

| Phase | Seconds | Offered RPS | Successful RPS | p50 ms | p95 ms | p99 ms | Unexpected % | Dropped |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Load | 300 | 400 | 399.85 | 33 | 136 | 265 | 0 | 0 |
| Stress | 100 | 500 | 485.62 | 734 | 1,430 | 2,341 | 0 | 887 |
| Stress | 100 | 1,000 | 444.76 | 1,070 | 1,940 | 2,951 | 0 | 54,909 |
| Stress | 100 | 2,000 | 456.55 | 1,045 | 1,590 | 2,951 | 0 | 153,511 |
| Stress | 100 | 4,000 | 414.41 | 1,148 | 2,259 | 3,293 | 0 | 357,935 |
| Stress | 100 | 8,000 | 396.66 | 1,194 | 2,218 | 3,611 | 0 | 759,741 |
| Stress | 100 | 10,000 | 384.73 | 1,211 | 2,410 | 3,857 | 0.008 | 960,810 |
| Spike baseline | 60 | 400 | 382.49 | 854 | 1,507 | 3,615 | 0 | 384 |
| Spike peak | 30 | 10,000 | 388.38 | 1,189 | 1,931 | 3,976 | 0 | 287,756 |
| Spike recovery | 90 | 400 | 374.30 | 1,153 | 1,772 | 3,900 | 0 | 1,659 |
| Soak minutes 0–10 | 600 | 400 | 336.67 | 1,340 | 2,449 | 4,940 | 0.178 | 36,737 |
| Soak minutes 10–20 | 600 | 400 | 270.05 | 1,680 | 4,257 | 5,916 | 1.598 | 74,456 |
| Soak minutes 20–30 | 600 | 400 | 253.47 | 1,769 | 4,671 | 6,029 | 2.734 | 82,913 |
| Soak minutes 30–40 | 600 | 400 | 256.40 | 1,746 | 4,510 | 6,026 | 2.860 | 80,783 |
| Soak minutes 40–50 | 600 | 400 | 247.29 | 1,790 | 5,096 | 6,080 | 3.513 | 85,237 |
| Soak minutes 50–60 | 600 | 400 | 234.32 | 1,880 | 5,595 | 6,149 | 4.512 | 91,839 |

Rate-limit rejects were **zero throughout**. All 23,979 unexpected outcomes were status-zero
transport/timeout failures, not received HTTP error responses. The HTTP timeout is five seconds after
dispatch; measured latency can exceed five seconds because it includes earlier scheduling and queueing.

The soak supplied 3,600 seconds of scheduled traffic over 3,620.428 seconds including draining and
short credential-renewal gaps. The API/container/database/cache/pool were retained throughout. Of
1,440,000 scheduled operations, 988,035 completed, 964,059 succeeded, 23,976 failed and 451,965 were
dropped. Overall successful throughput was **266.28 RPS** over that wall interval; unexpected error
rate was **2.427% of completed operations**. Latencies above are complete segment histograms, not an
invented whole-hour percentile. The completed category mix under overload is available per scenario
and can differ from the scheduled mix because admission drops work.

Across all phases, including warmup, calibration and separate scenarios, 4,501,250 operations were
scheduled; 1,471,693 completed, 1,447,714 succeeded and 3,029,557 were dropped before HTTP dispatch.
The generator's maximum observed scheduling delay was 88.63 ms and maximum sampled CPU was 5.00%
of eight logical processors. Its configured concurrency/queue cap is a material limit: this is not
evidence that 10,000 requests per second actually reached the API, or an isolated API capacity ceiling.

## Resource measurements

CPU below is normalized across the API's eight logical processors. Docker uses a different scale:
100% means one fully used CPU. DB columns are the largest observed **ten-second window** p50/p95/p99,
not percentiles reconstructed by averaging windows. GC collection deltas cover the sampled intervals;
runtime pause percentage is reported as supplied by the runtime.

| Phase | API CPU mean/max % | Peak working set MiB | GC gen0/1/2 delta | Max GC pause % | Max ThreadPool threads/queue | DB window p50/p95/p99 ms | Peak Docker CPU % |
|---|---:|---:|---:|---:|---:|---:|---:|
| Load | 18.58 / 20.48 | 314.27 | 1512 / 891 / 14 | 1.57 | 10 / 27 | 20 / 80 / 307 | 671.36 |
| Stress 10,000 | 21.11 / 22.89 | 340.50 | 156 / 148 / 18 | 6.01 | 12 / 71 | 57 / 213 / 2105 | 593.19 |
| Soak 1 | 19.45 / 22.33 | 337.18 | 997 / 968 / 134 | 7.31 | 12 / 55 | 65 / 202 / 3214 | 628.12 |
| Soak 2 | 16.75 / 18.86 | 341.08 | 812 / 745 / 118 | 7.30 | 11 / 56 | 71 / 225 / 3236 | 624.59 |
| Soak 3 | 16.09 / 17.80 | 343.32 | 775 / 610 / 115 | 7.22 | 12 / 50 | 78 / 348 / 3263 | 614.31 |
| Soak 4 | 16.19 / 18.46 | 348.96 | 790 / 611 / 111 | 7.16 | 12 / 24 | 81 / 635 / 3222 | 601.39 |
| Soak 5 | 15.72 / 17.71 | 354.71 | 760 / 531 / 108 | 7.10 | 12 / 46 | 89 / 946 / 3189 | 601.49 |
| Soak 6 | 15.44 / 17.16 | 348.25 | 794 / 515 / 166 | 7.04 | 13 / 57 | 82 / 281 / 3132 | 600.17 |

During load, sampled active connections peaked at 53 and pooled connections at 75. Stress and every
soak segment reached all 100 active connections, with no free connections in multiple samples.
This is strong pool-pressure evidence alongside SQL CPU use; increasing the pool alone is not a
measured remedy. ThreadPool queues were transient rather than continuously growing; the idle sample
returned to three threads and zero queued work. SQL recorded 13.3 seconds of aggregate THREADPOOL
wait during the soak, so brief SQL worker pressure also occurred.

Cache hits **0**, misses **0**, calls **0**: hit ratio is undefined. Source review confirms measured
feature handlers do not use the registered cache adapter. No cached-endpoint result is presented as
system capacity. SQL Server's own buffer/plan caches warm normally.

### Memory, connections and soak qualification

The first/last five-minute median working sets were **323.59/337.34 MiB**; managed-memory medians were
**70.44/73.98 MiB**. Regression slopes were +0.221 MiB/min working set and +0.100 MiB/min managed
memory. Minimum sampled heap sizes in those windows were 41.28/51.86 MiB. There were 759 gen2
collections across the sampled soak interval; the final segment peaked below the preceding segment's
working-set peak.

After the idle recovery window, working set fell to **306.14 MiB** and the last collected heap size was
44.95 MiB. All **100 pooled connections were free, zero were active, zero were reclaimed by GC**;
open database transactions were **zero** before and after the soak. These observations show no stuck
connection/transaction leak or runaway memory growth in this run. **A small upward memory-retention
trend remains, and a slow managed leak is not excluded without a longer run and retained-object
profiling. Memory-leak clearance is therefore qualified, not an unconditional certification.**

Reservation rows grew from 44,648 before the soak to 93,699 afterward. Used data-file space grew from
103,481,344 to 145,948,672 bytes; this is expected persistent test data, not a connection leak.
Staff audit rows remained zero under the existing customer/staff distinction.

## Actual SQL review and bottleneck evidence

Thirty retained read plans were captured using `LAST_QUERY_PLAN_STATS` and
`sys.dm_exec_query_plan_stats`; all thirty contain runtime counters. This is last-execution evidence,
not just estimated plans. See Microsoft's [last actual plan documentation](https://learn.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-objects/sys-dm-exec-query-plan-stats-transact-sql)
and [query profiling explanation](https://learn.microsoft.com/en-us/sql/relational-databases/performance/query-profiling-infrastructure).

| Plan | Query | Retained executions | Mean elapsed / CPU ms | Mean logical reads | Last actual finding |
|---|---|---:|---:|---:|---|
| [01](Part11.Evidence/plans/hot-01.sqlplan) | Unfiltered reservation COUNT | 74,297 | 556.22 / 47.93 | 457.76 | Index scan reads 93,689 rows for a single count |
| [02](Part11.Evidence/plans/hot-02.sqlplan) | Project page | 163,725 | 41.04 / 5.73 | 206 | Scans 1,000 projects; sorts/offsets before area join |
| [03](Part11.Evidence/plans/hot-03.sqlplan) | Admin reservation page | 709 | 2,854.01 / 415.05 | 3,211.07 | Scans 93,689 reservations, sorts to the offset, then joins the page |
| [04](Part11.Evidence/plans/hot-04.sqlplan) | Customer-scoped count | 147,017 | 4.26 / 0.48 | 8.50 | Existing customer index seek; last actor has two matching rows |
| [05](Part11.Evidence/plans/hot-05.sqlplan) | Unit page and availability | 134,424 | 4.57 / 1.23 | 211.75 | Project index seek and correlated availability check in one statement |
| [06](Part11.Evidence/plans/hot-06.sqlplan) | Current terms | 168,105 | 3.01 / 0.99 | 4 | One-row input; no useful reason to add an index for this dataset |

DMV execution counts/times describe the retained plan's cache lifetime and can reset on recompilation;
they are not a complete per-query history of the entire run. Aggregate elapsed/worker wait times also
overlap across concurrent requests. Plans 29–30 are low-frequency schema/setup reads, not traffic hot
paths. Credential batches and write batches are intentionally absent from the saved read-plan inventory;
reservation eligibility reads are included. Insert execution plans and allocation stacks were not captured.

No saved plan has a spill warning or missing-index suggestion. That does not make the existing access
paths optimal: plan 03 demonstrates the full-history scan/sort directly. Source review of the measured
handlers confirms DTO projection and AsNoTracking, two count/page commands for lists, and a fixed
three-command details path for scalar/amenities/images. Child collections are loaded independently,
without a per-parent network query loop or cartesian product. Large child cardinalities are outside
this one-image/one-amenity fixture and need separate payload testing.

During the soak, aggregate worker waits increased by **202,369,111 ms SOS_SCHEDULER_YIELD**, compared
with **620,479 ms WRITELOG**, **12,524 ms PAGEIOLATCH_SH**, and **3,462 ms combined sampled lock waits**.
These are concurrent-worker totals, not wall-clock durations or percentages. At most one blocked SQL
request was observed in a resource sample. CPU scheduling/query work dominates the measured lock/I/O
signals, with pool saturation amplifying HTTP latency. Pool/free/reclaimed interpretation follows
Microsoft's [SqlClient counter definitions](https://learn.microsoft.com/en-us/sql/connect/ado-net/event-counters).

## Recommended next changes and approval items

1. Address the exact total-count cost first. The current pagination contract counts all reservations
   on every admin list request. Decide whether exact fresh totals must remain, or approve a different
   pagination/count-freshness contract. No approximate count, stale private count or DTO change was invented.
2. With schema approval, benchmark a reservation ordering index matching ReservationDate descending
   and Id ascending, and then a project Name/Id access path. Confirm actual plans and insert/index
   maintenance cost. The ordering index alone does not eliminate the unfiltered exact count.
3. Measure payload bounds and explicitly approved caching for public DTOs; preserve private actor
   isolation and authoritative authorization. Current cache hit ratio cannot improve until handlers
   actually use an approved policy.
4. Profile allocations after SQL improvements. The load's sampled interval allocated about 38.6 GiB,
   while live memory stayed much smaller; repeated allocation/GC is a secondary cost, not proof of a leak.
   Investigate the small retention trend with retained-object profiles and a longer soak.
5. Repeat with a separate generator and documented production-like SQL/API resources, then tune bounded
   concurrency using measured latency. This shared host and 256-worker cap cannot establish isolated
   service capacity. Any scale-out result needs its own environment/workload evidence.

**Another benchmark is required.** No SQL/index, business, security, session, logging-security or
deployment rule was weakened. No optimization is claimed without a new measurement. No production
configuration, package, entity, migration or project structure was changed.

## Implementation and validation

| Added/changed path | Purpose |
|---|---|
| `src/Brouj.API/Diagnostics/PerformanceDiagnosticsService.cs` | Opt-in sanitized resource, EF command, SqlClient pool and cache sampling with typed ILogger |
| `src/Brouj.API/Program.cs` | Register diagnostics only when explicitly enabled |
| `src/Brouj.Infrastructure/Caching/DistributedCacheService.cs` | Untagged hit/miss counters, preserving cache behavior |
| `tests/Brouj.Infrastructure.IntegrationTests/Performance/` | Real HTTP/container fixture, bounded load generator, SQL evidence collector, execution test and four measurement tests |
| `scripts/performance/` | Repeatable smoke/full execution, offline evidence validation/summary and runbook |
| `docs/Brouj.Docs/Part11.Evidence/` | Complete sanitized run evidence and SHA-256 manifest |
| This report | Results, limits, bottlenecks, approvals and gate status |

Code comments explain isolation, unique write pairs, credential handling, bounded arrivals, histograms
and diagnostic redaction. C# control blocks use braces and async I/O carries cancellation tokens.
Filesystem/process setup and cleanup use the available synchronous OS metadata/launch APIs outside
timed request execution; artifact contents and HTTP/SQL I/O use asynchronous APIs.

Validation executed:

- Release solution build: passed, zero warnings/errors.
- Four measurement tests: passed; cover exact mix, error/reject separation, merged quantiles and stable-rate accounting.
- Smoke benchmark: passed execution validation with real SQL/HTTPS; explicitly excluded as full-duration evidence.
- Full 32-phase benchmark: passed execution completion over approximately 86 minutes. Capacity/SLO failure is reported above.
- Offline summary: passed request-accounting, approved-duration, sample-presence and actual-plan checks.
- Dependency vulnerability scan: no known vulnerable packages from configured sources.
- Full Release regression suite including SQL Testcontainers: **1,456 passed, zero failures/skips**.
- All forty SHA-256 manifest entries verified against the checked-in evidence copy: zero mismatches.
- `git diff --check`: passed.

Regression command:

```powershell
dotnet test Brouj.slnx --configuration Release --no-build --no-restore `
  --environment BROUJ_RUN_SQLSERVER_TESTS=1 `
  --filter 'FullyQualifiedName!~LocalPerformanceBaseline' `
  --logger 'trx;LogFilePrefix=Part11Regression'
```

The opt-in long benchmark was run separately, not skipped as a substitute for performance execution.
The pre-existing deletion of `scripts/security/Verify-TrackedSecrets.ps1` was preserved.

| Regression project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain.UnitTests | 24 | 0 | 0 |
| Application.UnitTests | 528 | 0 | 0 |
| Infrastructure.IntegrationTests | 506 | 0 | 0 |
| API.IntegrationTests | 398 | 0 | 0 |
| **Total** | **1,456** | **0** | **0** |

The Infrastructure run completed in 4 minutes 25 seconds and includes the existing security,
transaction and concurrency coverage against Testcontainers. Regression TRX files are under each
test project's ignored `TestResults/Part11Regression_*.trx`; benchmark TRX files use their UTC run IDs.

## Definition of Done / gate

| Requirement | Result |
|---|---|
| All four performance test types executed | Complete: load 300 s, stress 600 s, spike 180 s, soak 3,600 scheduled seconds |
| Mixed production-like workload tested | Complete for the documented synthetic subset; exact category mix met during the passing load |
| Environment documented | Complete; local shared-host baseline, not production certification |
| Latency/error/resource metrics recorded | Complete; raw samples, phase histograms/quantiles, pool/cache/rate-limit outcomes retained |
| Hot SQL reviewed | Complete for the captured hot read/eligibility plans; write-plan capture limitation stated |
| No memory/connection leak in soak | No stuck connection/transaction or runaway growth observed; slow memory-leak clearance remains qualified |
| Performance target proven or failure documented | Complete: target not achieved, long-soak SLOs failed, measured bottleneck and follow-up decisions documented |

**Capacity acceptance: NOT MET.** Memory-leak clearance is qualified by the small retention trend and
absence of retained-object profiling. The current local baseline does not justify 9k–10k RPS or
hour-long stable 400 RPS claims. Further benchmarking and owner decisions on pagination/index changes
remain necessary. Work stops at Part 11.
