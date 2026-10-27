# Brouj Backend V1 — final verification

Review date: 2026-10-28. Scope: Part 12, existing implementation and release handoff. No frontend or
production deployment was performed.

| Required result | Status |
|---|---|
| **PASS** | Backend V1 implementation and frontend handoff, with the owner-approved exceptions below. All 1,465 regression tests passed. |
| **FAIL** | The historical 9k–10k RPS target and hour-long 400 RPS SLOs remain unmet; no capacity claim is made. |
| **BLOCKERS** | No unresolved code decision found for frontend handoff. Target-environment configuration remains required before deployment. |
| **Known accepted risks** | Owner explicitly deferred cache integration and heavy-query/performance work, and separately accepted existing synchronous local file-metadata I/O for V1. |
| **Tests run** | Release build; 1,465 full-suite cases, zero failed/skipped; nine new focused architecture/migration/HTTPS cases; dependency and source/security checks. |
| **Benchmark result** | Part 11 completed all four test types. Initial five-minute load: 399.85 successful RPS, p95 136 ms, p99 265 ms. Hour-long soak failed its SLOs. |
| **Security result** | Reviewed application controls and focused real-HTTP ownership/session checks pass; actual production edge, TLS, secret hosting and SQL privileges remain deployment checks. |
| **Migration status** | Clean migration, snapshot consistency, physical schema comparison and data-preserving reapplication passed against SQL Server Testcontainers. |
| **Out-of-scope changes** | None found in the reviewed backend; no new business feature, package, entity, project reference or schema change was introduced. |

## Owner decisions and limits

The owner authorized completing V1 and proceeding to frontend work despite the known Part 11 capacity
failure, unused feature cache and expensive admin queries, with further testing/optimization planned
after deployment. The subsequent explicit answer **“Accept the documented V1 limitation and proceed”**
approved synchronous directory/file metadata operations in the existing local upload provider. That
exception is restricted to those filesystem operations; it does not waive database/HTTP async I/O or
security requirements.

The [Part 11 report](Part11.PerformanceVerification.md) and its unchanged evidence remain the capacity
record. The small memory-retention trend and qualified slow-leak clearance remain visible. This review
does not turn that qualification into proof of no leak or claim that caching would achieve 10k RPS.
No new 86-minute benchmark was needed for the test/documentation-only verification changes.

## Checklist review

| Part 12 checklist area | Evidence / disposition |
|---|---|
| Clean Architecture dependencies; independent Domain; no HttpContext in Application | Six new architecture checks verify exact project-reference direction, Domain/framework separation, Application ASP.NET separation and response DTO boundaries. Source search finds HttpContext only in API/Infrastructure. |
| No Generic Repository, generic UnitOfWork or ASP.NET Identity | Project/package and source review pass. DbContext remains the unit of work, accessed through IApplicationDbContext. |
| Thin controllers; correct business-logic placement | Controllers bind explicit transport DTOs and dispatch MediatR requests. The existing contract tests cover all 101 approved operations and policies; implementation remains in Application/Domain. |
| Dedicated mappings and approved database structure | Existing model tests cover all 26 entity configurations, 181 columns, 22 FKs and 47 secondary indexes, including uniqueness, filtered indexes, delete behavior and shared initiative keys. The new migration test compares the physical migrated schema with this separately verified model. |
| No unapproved defaults/checks/uniqueness; enum strings; JSON audit mapping | Existing metadata, enum persistence, constraint and audit tests cover the contract. Audit JSON remains nullable nvarchar(max); no new default/check/alternate key was added. |
| Clean migration and reviewed SQL | New tests exercise MigrateAsync rather than substituting EnsureCreated, compare tables/columns/PKs/FKs/indexes/checks/defaults, verify no pending model changes, and reapply without removing a saved city. SQL artifacts are linked below. |
| Actual hot SQL reviewed | Part 11 contains thirty last-actual read/eligibility plans, count/paging bottleneck evidence and explicit write-plan limitations. Those findings remain deferred, not silently marked optimized. |
| Auth; user/customer management; cities/areas; projects/units | Existing feature/HTTP/persistence tests and the 101-operation inventory cover these families. The new migrated HTTPS test adds persisted customer registration/login/refresh/logout and an admin city write/public read. |
| Reservations/orders; time plans; templates; terms; activity-log queries | Existing real-SQL, feature and HTTP coverage passes through the full suite. New HTTPS coverage verifies reservation creation/duplicate rejection/cancellation, staff order creation, server-derived total/status, ownership denial and persisted staff auditing. |
| Parts 5/10 security, IDOR/BOLA, injection and tokens | Existing security and concurrency suites are rerun against SQL Server. The earlier Part 5 database-verification gap is superseded by later real-SQL runs and this final run. No new unresolved Critical/High code finding was identified. |
| XSS/CSRF/CORS; files/path traversal; SSRF | Production-mode real HTTPS tests obtain identity-bound CSRF proof and secure cookies. Existing HTTP/file tests cover the other boundaries. No application remote-fetch feature exists requiring an SSRF destination policy. |
| DoS limits; safe errors/logs; secrets/dependencies | Existing per-endpoint rate/payload/page/depth controls, fail-closed configuration and sanitized production errors remain. Current-source review and vulnerability scan results are below. Production must supply actual budgets and edge configuration. |
| Statelessness and horizontal scaling | No process-local business lock/session correctness dependency found. Scale-out requires shared protected Data Protection keys, consistent upload storage and aggregate edge controls; this is an application-design review, not an executed multi-instance certification. |
| Cache compatibility; N+1; pagination | External IDistributedCache remains supported; feature cache use is owner-deferred. Reviewed hot reads use DTO projections and bounded lists with fixed query counts. Details' larger child cardinalities remain outside the Part 11 fixture. |
| No blocking I/O | Owner-approved exception for local filesystem metadata/open/delete operations. No sync database/HTTP I/O or task blocking was found in production paths. |
| Performance tests complete; claims supported | Part 11 load/stress/spike/soak evidence retained. Failed capacity is an explicit owner-accepted V1 limitation. No 9k–10k RPS claim is made. |
| All test layers, security/concurrency and critical end-to-end flows | Full-suite results below plus the new migration-backed Production HTTPS workflow. No HTTP/persistence/session doubles are used in that workflow. |
| No unapproved features or schema | No Identity, generic repository/UoW, payment/draw engine/results, TimePlanTemplate, customer activity-log module, IsSold column, separate initiative APIs, EAV/dynamic attributes or extra application tables found. |

## Migration release artifacts

- [Forward initial migration SQL](Part12.Migrations/initialMigration.sql).
- [Idempotent initial migration SQL](Part12.Migrations/initialMigration.idempotent.sql).
- Migration ID: `20261026163251_initialMigration`; EF migration product version: `10.0.14`.
- Forward SQL SHA-256: `9EA15D35EFB92D8A94504F1B7F1CEF8826F9DD5FEE1464DBF515516C09F56024`.
- Idempotent SQL SHA-256: `84EA6B3870B808D9F5241485222C318A03BBC4E26E744C75C9858B89AB7091BC`.

Reviewed forward SQL creates 26 application tables plus EF's migration-history table, 47 secondary
indexes and 22 FKs. It adds no column defaults or check constraints and contains no DROP statements.
Applying the migration to a clean disposable SQL database succeeded; applying it again preserved data
and did not duplicate migration history. The existing migration's Down method drops the initial schema;
it is not a data-preserving production rollback strategy.

No existing developer or production database was migrated or reset. Apply reviewed SQL using a
separate deployment identity; keep runtime credentials limited to required DML. EnsureCreated is not
a replacement for migration verification or an upgrade path for a pre-existing untracked schema.
This follows Microsoft's [migration deployment guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
and [EnsureCreated documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/ensure-created).

## Tests and security checks run

The Release build passed with zero warnings/errors. Six new API architecture cases and three new
Infrastructure migration/HTTPS cases passed in focused runs. The HTTP test has a thirty-second
functional timeout to allow cold EF/password initialization; the earlier benchmark's five-second
timeout and performance evidence are unchanged. Test helpers use ILogger<T> for safe stage messages.

```powershell
dotnet build Brouj.slnx --configuration Release --no-restore
dotnet test Brouj.slnx --configuration Release --no-build --no-restore `
  --environment BROUJ_RUN_SQLSERVER_TESTS=1 `
  --filter 'FullyQualifiedName!~LocalPerformanceBaseline' `
  --logger 'trx;LogFilePrefix=Part12Final'
dotnet list Brouj.slnx package --vulnerable --include-transitive --no-restore
```

The long opt-in benchmark is excluded here because its complete historical run is already recorded;
SQL tests are enabled, not skipped. The functional flow starts the actual Release API in Production
over pinned local HTTPS and runs on a migrated Testcontainer database with the real session service.

| Test project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Domain.UnitTests | 24 | 0 | 0 |
| Application.UnitTests | 528 | 0 | 0 |
| Infrastructure.IntegrationTests | 509 | 0 | 0 |
| API.IntegrationTests | 404 | 0 | 0 |
| **Total** | **1,465** | **0** | **0** |

The full SQL run completed in 4 minutes 36 seconds, including existing security/concurrency coverage
and the new migrated HTTPS flow. TRX evidence is under each project's ignored
`TestResults/Part12Final_*.trx`. The six architecture cases and three migration/HTTP cases add nine
tests to the previous 1,456-case regression suite.

Scoped whitespace verification of the three new C# test files and `git diff --check` passed. Both
archived SQL files match the SHA-256 of the SQL regenerated during the final test run.

The dependency scan reported **no known vulnerable packages** from configured sources. A targeted scan
of 617 current runtime source/config/project files found no literal password/signing-secret values,
password-bearing connection-string literals, private keys, JWT credentials or AWS access-key matches.
This is a targeted source check, not a repository-history certification. The old bootstrap credential
already documented in Part 10 remains retired and must not be reused. Existing tests verify credential,
identity-data and provider-exception log redaction; sensitive EF/IdentityModel logging remains disabled.

Initial focused runs exposed stopped Docker Desktop and two test-harness issues (catalog-text
collation and a cold-start HTTP timeout). Docker was started, the SELECT metadata comparison was
normalized without changing database collation, and only the functional client timeout was adjusted.
Focused migration and real HTTPS tests then passed.

## Frontend and deployment handoff

Frontend work can use the existing [endpoint contract table](Part8.EndpointContracts.md),
[machine-readable contracts](Part8.EndpointContracts.json), and Development Swagger at
`https://localhost:7249/index.html`. Launch locally with the HTTPS profile after supplying the required
configuration. Read the [API configuration/browser sequence](Part8.ApiConfiguration.md) and
[initial administrator guide](Part10.InitialAdmin.md).

For browser integration, send credentials, obtain `/api/v1/auth/csrf`, attach `X-CSRF-TOKEN` on unsafe
requests and obtain a fresh proof when authentication changes. Tokens remain in HttpOnly cookies.
Read profile DTOs for UI state; handle both application error envelopes and Problem Details, 409
conflicts and 429/Retry-After. List responses' `data` contains `items`, `pageNumber`, `pageSize`,
`totalCount` and `totalPages`, with page size limited to 100. Use the documented route policy rather than inferring
access solely from an `/admin/` URL prefix. Frontend validation complements server authorization.

Before deployment, supply the real connection/secret provider, issuer/audience/signing key, canonical
HTTPS host/origins, explicitly trusted proxies, request/depth/rate budgets, safe persistent upload
storage and appropriate SQL privileges. Configure the protected Data Protection key store and edge
controls for the actual topology. Media paths are metadata; choose safe media hosting before relying
on them as public URLs. `/health/live` is liveness only, not a database-readiness check. Production
does not expose Swagger, and startup does not automatically apply migrations.

These are existing deployment inputs, not waived security controls. No production target was supplied
or deployed. The planned post-deployment testing can measure/tune performance once the application
is configured securely; its results must be recorded before making a capacity claim.

## Files changed and final disposition

- Added `Part12ArchitectureTests.cs`, `Part12MigrationTests.cs` and `Part12ReleaseHttpTests.cs` in the
  existing API/Infrastructure test projects; no new project, reference or package.
- Extended the SQL fixture and existing real-process benchmark fixture with an explicit migration
  option, preserving their existing default paths.
- Added this report and the two generated/reviewed migration SQL artifacts; updated README handoff links.
- Preserved the owner's pre-existing handler/cache formatting changes; removed two trailing-whitespace
  lines in the cache file only. No cache integration or business behavior was changed.

V1 is eligible for frontend handoff with the explicit owner-approved performance and file-I/O
exceptions above. Actual production deployment verification remains tied to the target environment.
Stop at Part 12; frontend implementation and performance optimization are separate work.
