# Part 6 performance foundation review

Review and targeted fixes are complete. **The gate has two outstanding qualifications: live SQL verification is blocked, and local file storage still uses synchronous filesystem metadata operations.** No 9,000–10,000 RPS result is claimed. Only Part 6 and the existing foundation were reviewed; later Part files were not opened.

## PASS items

| Area | Finding |
|---|---|
| Stateless correctness | No in-process session, process-local business lock or mutable static correctness store was found. Refresh correctness uses database transactions/locks/indexes. Private static serializer options and format dictionaries are configuration, not session state. |
| DbContext lifetime | Scoped Application/concrete context remains the effective unit of work. Factory-created refresh contexts have independent tracked state and disposal. Added a test proving that creating them does not replace or share the request context. |
| Sequential context use | Fixed concurrent validator execution. Validators are now awaited in registration order so scoped database operations cannot overlap through this pipeline. No production `Task.WhenAll`, `.Result`, `.Wait()`, blocking task unwrap or unbounded task queue remains in the reviewed source. |
| EF read conventions | Existing refresh reads use `AsNoTracking` and scalar projections. Tracking remains available for writes; no global tracking change was needed. Domain entities are sealed, no lazy-loading package/proxy registration exists, and no lazy-loading pattern was found. |
| Index definitions | Reviewed all 47 approved secondary indexes against model metadata and generated SQL Server DDL. No exact duplicates or clearly removable unfiltered prefix indexes were found. Schema/index definitions remain unchanged. |
| Cache contract | `IDistributedCache` adapter supports an external provider, scoped keys, bounded TTL and explicit removal. Added a test for filter/page/language variants across service instances, selective invalidation and private/anonymous separation. |
| Transactions | Validation precedes the transaction behavior. Only marked requests enter that boundary. Refresh sessions use short, sequential database operations with independent retry contexts; no file upload, password hashing or remote HTTP call occurs inside their transaction. General nested transactions join the existing boundary. |
| Cancellation | Database/cache/file-stream operations accept and propagate tokens. Validation now checks cancellation before starting, between validators and before the handler. |
| Optimization scope | No new pooling, compiled queries, indexes, cache backend, parallel worker, observability package or benchmark project was introduced. Existing security costs and checks remain intact. |

## Changes made

`ValidationBehavior` previously started validators together and reused one FluentValidation context. Regression tests demonstrated overlapping operations, duplicated errors and cancellation gaps. The correction awaits validators sequentially, creates an independent validation context per validator, aggregates each result once, and stops downstream work when cancellation is observed. Comments explain the scoped DbContext and shared failure-list constraints. Method declarations follow the supplied `IJwtTokenService.cs` format.

| Added/changed file | Purpose |
|---|---|
| `src/Brouj.Application/Behaviors/ValidationBehavior.cs` | Sequential, cancellation-aware validation with independent failure state. |
| `tests/Brouj.Application.UnitTests/Behaviors/ValidationBehaviorTests.cs` | Six regression cases for ordering, error aggregation and cancellation. Four failed against the previous implementation; all pass after the fix. |
| `tests/Brouj.Infrastructure.IntegrationTests/DependencyInjectionTests.cs` | Verify factory/request context and change-tracker isolation without a database connection. |
| `tests/Brouj.Infrastructure.IntegrationTests/Services/CacheTests.cs` | Verify dimensioned keys, cross-instance contract behavior, invalidation and actor isolation. |
| `tests/Brouj.Infrastructure.IntegrationTests/Model/FoundationSqlReviewTests.cs` | Six offline DDL/translation cases; emit inspected SQL into test output. These verify representative query shapes and provider mappings, not live execution or query plans. |
| `docs/Brouj.Docs/Part 6 Performance Review.md` | Review evidence, risks, observability readiness, gate status and deferred benchmark requirements. |

Existing Part 5 working-tree changes were preserved. No database schema, package reference, project structure, API endpoint or business feature was changed. No commit or PR was created.

## Indexes reviewed and generated SQL

The complete reviewed inventory is [ApprovedIndexes.tsv](../../tests/Brouj.Infrastructure.IntegrationTests/Fixtures/ApprovedIndexes.tsv). It contains 47 secondary indexes across 24 tables; the model also defines primary keys for all 26 entities. There are **no migration or model-snapshot files** in the source tree, so there were no migration indexes to inspect. Reviewed the model's `GenerateCreateScript()` output, which is also the schema path used by the existing `EnsureCreatedAsync` fixture. This does not prove that any deployed database matches it.

| Group | Count | Review |
|---|---:|---|
| ActivityLogs | 4 | CreatedAt plus user/entity/action history indexes. |
| Customers, Users | 7 | Name indexes and filtered unique live-contact/identity indexes. |
| CustomerRefreshTokens, UserRefreshTokens | 4 | Unique token lookups and filtered unique active-session indexes per actor. |
| Reservations, Orders | 8 | Customer/unit/status lookups; number uniqueness; processing reservation and order/reservation uniqueness. |
| Geography, projects, units and amenities/images | 10 | Approved names, types, ownership/FK prefixes and project/unit numbering. |
| Plans, stages, stage items and images | 5 | Project ownership and parent/sort-order/status access paths. |
| Templates and template links | 7 | Active/code lookups, link uniqueness, ordering and remaining ItemTemplate FK index. |
| TermsAndConditions | 2 | Active/effective-date access and filtered active-version uniqueness. |

Filtered unique indexes retain different semantics from unfiltered access indexes. Composite indexes already cover several FK prefixes, and the prior redundant refresh/order/time-plan FK indexes remain absent. Low-selectivity status/active indexes require workload evidence before changing them. No index was removed on speculation.

Representative SQL was generated and inspected offline using the same shapes as existing persistence/service reads. Captured output is in `FoundationSqlReviewTests` results under the ignored `TestResults/Part6Final*.trx` and `Part6SqlReview*.trx` files. Observations:

- The existing audit timestamp test's city lookup uses a GUID predicate and a bounded two-row cardinality check.
- Both refresh lookups select only ID, actor ID, active flag and expiry. Their digest parameters are ANSI `varchar(450)`, matching the approved token-column type; there is no join or extra timestamp projection.
- Both actor-lock queries retain fixed `UPDLOCK, HOLDLOCK` hints and a GUID parameter. Their outer projections request deletion state and the staff role where applicable; the raw inner statement still contains `SELECT *`. SQL Server's actual plan and column pruning remain unverified.

Example generated customer refresh lookup, with synthetic parameter values omitted here:

```sql
SELECT TOP(@p) [c].[Id], [c].[CustomerId], [c].[IsActive], [c].[ExpDate]
FROM [CustomerRefreshTokens] AS [c]
WHERE [c].[Token] = @digest
```

## Async/blocking issues and performance risks

| Risk | Review outcome / follow-up |
|---|---|
| Synchronous filesystem operations | `Directory.CreateDirectory`, `DirectoryInfo.Exists/Attributes`, `File.Exists/GetAttributes/Delete` and file opening remain synchronous OS operations. Stream reads/writes/flush use asynchronous APIs. The strict “no blocking I/O” DoD item is **not met**. `Task.Run` would still block workers; removing traversal checks or reliable cleanup would weaken security. This review retains the existing provider and records the exception. |
| Local storage across instances | Upload references require a suitably shared mounted root and exclusive service-account permissions. A slow/shared filesystem can increase metadata latency. Storage topology and the synchronous-I/O exception require owner review or a separately approved storage-provider change. |
| Cache deployment and invalidation | Memory is the development default; shared deployments need an external `IDistributedCache` provider. The test double validates the contract, not Redis/network behavior. Removal addresses one complete key in one actor scope; feature-specific cross-key/cross-actor invalidation must be designed explicitly. |
| Cache allocations/cardinality | JSON is serialized before its 1 MiB size check; oversized source DTOs can allocate more than the accepted payload size. Distinct scoped/filter/page keys can grow rapidly. Existing TTL/depth/payload limits help but do not bound total provider memory. Features must choose small DTOs and canonical bounded keys; measure misses, allocations and provider memory. |
| Password CPU/memory | Argon2 is intentionally synchronous CPU work. Part 4's historical short benchmark reported roughly 111–113 ms and 118.58 MB cumulative managed allocation per operation; it was not rerun here and is not peak memory or throughput evidence. Authentication concurrency/rate limiting must be measured without reducing security parameters. |
| Refresh database pressure | Multiple statements, actor-row locks, retries and audit writes can amplify latency/pool demand. Contention for one actor is intentional. Inactive token/audit history growth and retention policy need workload review. No cleanup policy or index change was invented. |
| Audit and logging cost | Audit snapshots/change detection scale with tracked changes. Application emits request-start/completion logs; sink backpressure and volume need load testing. Keep failure redaction and disable sensitive-data logging while measuring. |
| Deployment state | Antiforgery Data Protection keys need a shared protected key ring. Authentication/session correctness must never depend on cache availability or one host's memory. |
| Future external calls | None exist in the current foundation. Any HTTP integration must use `IHttpClientFactory`, finite timeouts, cancellation and bounded response work; do not hold database transactions across it. |

## Observability readiness

The architecture can expose all required measurements through existing framework diagnostics, service boundaries and EF interceptors. A collector/exporter/dashboard has not been configured; readiness is not a claim that these measurements are already collected. Add no credentials, full URLs, raw SQL, cache keys, actor IDs or other unbounded/sensitive tags.

| Required measurement | Collection path and current gap |
|---|---|
| RPS, p50/p95/p99, 4xx/5xx | ASP.NET Core request-duration/count measurements and status dimensions; configure collection and histogram buckets for load tests. |
| DB latency | EF command diagnostics/interception or SqlClient tracing; collect sanitized timing/outcome data. No latency collector exists yet. |
| DB pool usage | SqlClient event counters for active/free/pooled connections; collect alongside SQL Server waits/locks during live tests. |
| Cache hit/miss | `ICacheService`/adapter is a single instrumentation boundary; counters and provider telemetry are not implemented yet. Distinguish provider failures/corrupt entries from ordinary misses. |
| CPU, memory, GC, ThreadPool | Built-in `System.Runtime` metrics; collect process CPU/working set, allocation/GC activity and worker/queue behavior. |
| Rate-limit rejects | ASP.NET Core rate-limiter diagnostics when endpoint policies/middleware exist; current host has no rate limiter configured. |

Framework support and the requirement to avoid parallel operations on one DbContext were checked against Microsoft's [DbContext guidance](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/), [ASP.NET Core metrics](https://learn.microsoft.com/en-us/aspnet/core/metrics/built-in?view=aspnetcore-10.0), [SqlClient counters](https://learn.microsoft.com/en-us/sql/connect/ado-net/event-counters?view=sql-server-ver17) and [runtime metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime). Index conclusions are source-review inferences, not measured plan results; Microsoft's [query guidance](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) also calls for execution-plan evidence before tuning.

## Tests executed and blocking design issues

Final suite:

```powershell
dotnet test Brouj.slnx --configuration Release --no-restore --logger 'trx;LogFilePrefix=Part6Final'
```

| Project | Passed | Failed | Skipped SQL methods |
|---|---:|---:|---:|
| Domain.UnitTests | 24 | 0 | 0 |
| Application.UnitTests | 32 | 0 | 0 |
| Infrastructure.IntegrationTests | 374 | 0 | 27 |
| API.IntegrationTests | 6 | 0 | 0 |
| **Total** | **436** | **0** | **27** |

Added 14 executable cases. The focused validator suite passed 9/9 after the fix; the focused SQL/cache/factory review passed 8/8. Release solution build and `git diff --check` passed. Dependency scanning (`dotnet list Brouj.slnx package --vulnerable --include-transitive`) reported no known vulnerable packages from configured feeds.

The validator design issue is resolved. Live SQL verification remains blocked under the owner's prior instruction to continue with available checks: no test connection or container-test flag is configured. The 27 skipped methods include persistence, refresh concurrency/replay, constraints and audit behavior. No database selection/startup, fake relational substitute or deployment change was attempted. Physical index presence, execution plans, lock waits, SQL timings and pool saturation remain unverified.

No new design decision was needed for the correction. Unconditional gate approval still requires owner review of the filesystem-I/O exception and closure or explicit acceptance of the live-SQL blocker. Earlier Part 5 qualifications remain visible in its report.

## Definition of Done

| Item | Result |
|---|---|
| Foundation stateless | PASS design; deployment requires external cache where shared hits are needed, shared upload storage and Data Protection keys. |
| No obvious scalability blocker | Validator overlap fixed; no further confirmed algorithmic blocker identified. Live database and storage performance remain unverified. |
| Caching distributed-ready | PASS contract/configuration/tests; real distributed backend behavior deferred. |
| EF suitable for high read load | PASS source/translation/lifetime review; feature query plans and load verification deferred. |
| No blocking I/O | NOT MET: synchronous filesystem metadata/open/delete operations remain. No sync database I/O or task blocking found. |
| No premature micro-optimization | PASS; changes correct validation/resource use without speculative tuning. |
| Part 11 benchmark requirements documented | PASS, below. |

## Deferred benchmarks for Part 11

These requirements come from this Part's review; the Part 11 plan was not opened or implemented.

- Establish a representative seeded SQL Server dataset, approved endpoint/request mix, topology, hardware/runtime/GC configuration, connection-pool settings, cache provider and warm/cold state. Define workload-specific latency/error budgets with the owner before pass/fail claims.
- Run warmup, steady load, concurrency ramps, burst/overload and soak tests. Treat 9,000–10,000 RPS as an experimental target; report achieved throughput, p50/p95/p99, 4xx/5xx, timeouts, cancellations and rate-limit rejects. Keep load-generator limitations visible.
- Capture DB latency/round trips, actual execution plans, logical reads, lock/deadlock waits, active/free pool connections and retry amplification. Recheck index selectivity, overlap and write cost before proposing changes to approved indexes.
- Measure representative read projections, pagination and filtered requests for over-fetching/N+1 behavior; include realistic largest allowed payloads and late pages. Do not invent business queries in this gate.
- Measure anonymous/customer/staff cache isolation, filter/page/language keys, cold/hot hit rates, invalidation freshness across instances, stampedes, provider failures and total memory/cardinality. No authorization/session correctness may use cached state.
- Exercise refresh/login contention, audit writes and long-running/cancelled operations under load. Re-run Release password benchmarks with longer measurements and include auth concurrency, allocations, CPU and memory without weakening hashing or security controls.
- Exercise file upload/delete/failure cleanup on the chosen local/shared storage under concurrency; measure metadata delays, streamed throughput, cancellation, orphan compensation and worker pressure.
- Collect CPU, working set, allocation rate, GC activity, ThreadPool workers/queues and logging/exporter overhead. Compare multiple instances with shared storage/protection keys and verify failure/restart behavior.
- Retain reproducible commands, configuration, raw output, dataset size, duration and limitations. Finish blocked relational correctness tests before interpreting load-test success as correctness evidence.
