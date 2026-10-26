# Part 8 API implementation report

Implemented on 2026-10-26 using the owner's approved browser topology, configuration-only traffic budgets, preserved permissions and instruction to continue without SQL Server/Docker/Testcontainers.

## Scope and changed files

- All **101 approved Application requests** have one controller action each. The API contains 26 business controllers plus a common result adapter and CSRF/liveness controllers. Controllers construct explicit requests, dispatch `ISender.Send` with cancellation and map results; upload actions also own/dispose the transport stream. They contain no persistence queries or business decisions.
- Added API policy names/registration, 37 explicit HTTP request/query/CSRF response DTOs, startup configuration validation, endpoint rate policies, upload validation, counted request streams, safe error/result mapping, security headers and structured request/failure logs.
- Updated `Program.cs` and environment settings to compose the pipeline and approved origins/HTTPS host configuration. Secure host-only Strict cookies and the existing trusted `CurrentUser`, JWT validation, session adapter and CSRF services are reused.
- Staff management remains **SuperAdmin-only** at both the route and Application boundaries. Other approved administration permits Admin/SuperAdmin; project time-plan reads preserve active-customer/staff access.
- Added three API test suites and isolated test support, and updated the existing authentication tests to use shared synthetic host settings and required braces.
- No project/package references, Domain/Application/Infrastructure source, schema or unrelated features were changed. No future Part was inspected or implemented.

See the [complete changed-file inventory](Part8.ChangeInventory.md), [endpoint contract table](Part8.EndpointContracts.md), [machine-readable contract](Part8.EndpointContracts.json) and [required deployment configuration](Part8.ApiConfiguration.md).

## HTTP behavior and hardening

The endpoint contracts document each HTTP method/route, MediatR request, body/query DTO, response DTO, route policy, ownership location, success/error mapping, rate category and cache behavior. All 101 routes are exercised through WebApplicationFactory and their metadata/DTO types are checked against that contract. Successes use the existing `ApiResponse<T>` envelope; controlled Application failures use `ApiErrorResponse`. Transport/model-binding errors and unexpected failures use safe Problem Details.

Validation maps to 400; protected authentication to 401; forbidden access to 403; missing/non-owned resources to 404; business/optimistic-concurrency conflicts to 409; request/file excess to 413; unsupported content types to 415; rate rejection to 429. Unexpected failures return 500 without stack traces, SQL, internal paths or exception messages. The small API MediatR behavior preserves a sanitized concurrency marker before the existing Application provider-error sanitizer; it adds no transaction, locking, race handling or retry behavior.

Protected use cases continue to verify live database account/role state in `AuthorizationBehavior` and `FeatureAccess`. Ownership remains inside the existing handlers. Request JSON cannot bind unknown actor/permission fields, and path identifiers are assigned explicitly. Unit detail/update/delete retain the approved required project scope through a `projectId` query parameter.

CORS permits only configured exact HTTPS frontend origins with credentials. The browser must bootstrap and send the framework X-CSRF-TOKEN proof on all unsafe operations, including anonymous login and expired-access refresh. Identity changes require a new proof. Because CSRF runs before authorization, a missing proof returns 400 before an otherwise applicable 401/403; authorization tests provide valid proofs.

All five rate categories require configured positive permits/windows. JSON/multipart body and JSON-depth budgets are mandatory, with no production traffic guesses. Byte counting also protects requests without Content-Length. Individual image files remain capped at 5 MiB, with one file per request; existing storage code checks signatures, metadata and safe generated filenames. Limits execute before form/CSRF parsing. Forwarding is disabled with empty trust lists and otherwise accepts only explicitly configured proxy addresses; forwarded Host is never consumed.

Structured logs contain route templates, status/feature error/failure type, elapsed time and server trace IDs. They exclude exception objects/messages, request bodies, query strings, credentials and identity values. Response headers deny framing, prevent sniffing, restrict browser capabilities and use no-store. Production OpenAPI is disabled. Health reports fixed liveness only. Public read caches are not enabled by this Part.

## Tests and checks

| Check | Final result |
|---|---:|
| Solution build | Passed; zero warnings / errors |
| API integration tests | **358 passed** |
| Application unit tests | **518 passed** |
| Domain unit tests | **24 passed** |
| Infrastructure tests available without SQL Server | **375 passed** |
| Total available tests verified | **1,275 passed, 0 failed** |
| Existing SQL Server-dependent tests | **31 Blocked/Pending** |
| Dependency vulnerability scan, including transitive dependencies | No known vulnerable packages reported by configured NuGet sources |
| Authored-file formatting and whitespace/diff checks | Passed |

The full solution test invocation passed all then-current tests. After the final API-only checks/edits, the API suite was rerun successfully with 358 tests. Other layers were unchanged. Final API results are in the ignored `tests/Brouj.API.IntegrationTests/TestResults/part8-final.trx`; the other projects' results are in their ignored `TestResults/part8-full.trx` files.

API test coverage includes every route/dispatch/DTO/policy/rate category, common error mapping, unauthorized/wrong-realm requests, actual Application ownership filters and cancellation rejection, SuperAdmin access, stale/deleted account checks, pagination and its maximum, unit project scope, login/refresh/logout cookie transport, refresh replay rejection through a session double, CORS preflights/credentials, CSRF bootstrap/identity changes, known-length and streamed oversized JSON, nesting/unknown fields, content types, file count/size/signature/type/path rejection, HTTPS/host behavior, proxy trust and sanitized production responses/logs.

Controller-contract tests replace `ISender` to verify transport wiring and status mapping only. Application HTTP tests retain the real MediatR pipeline and handlers with query/transaction/session doubles. These tests do **not** establish SQL translation, database constraints, persistence, rollback, refresh locking or concurrent correctness. Infrastructure's available password/JWT/cookie/storage tests were also rerun. No EF InMemory substitute is presented as relational proof.

Commands used:

```powershell
dotnet build Brouj.slnx --no-restore -m:1 -clp:ErrorsOnly

$env:BROUJ_RUN_SQLSERVER_TESTS = '0'
$env:BROUJ_SQLSERVER_TEST_CONNECTION_STRING = ''
dotnet test Brouj.slnx --no-restore -m:1 -clp:ErrorsOnly --logger 'trx;LogFileName=part8-full.trx' --logger 'console;verbosity=quiet'

dotnet test tests/Brouj.API.IntegrationTests --no-restore -m:1 -clp:ErrorsOnly --logger 'trx;LogFileName=part8-final.trx' --logger 'console;verbosity=quiet'
dotnet list Brouj.slnx package --vulnerable --include-transitive
dotnet format whitespace Brouj.slnx --no-restore --include src/Brouj.API tests/Brouj.API.IntegrationTests
git diff --check
```

The sandbox prevented the first build/test-host processes from communicating normally. Required build/test/format checks were rerun with process access outside the sandbox and passed. No Docker, Testcontainers runtime, SQL Server target or deployment database was installed, started or used.

## Blocked/Pending database verification

These existing SQL Server tests were explicitly skipped because no SQL target is available and the owner prohibited Docker/Testcontainers use:

| Existing SQL Server test class | Pending cases |
|---|---:|
| `RefreshTokenPersistenceTests` | 8 |
| `AuditPersistenceTests` | 7 |
| `ApplicationFeaturePersistenceTests` | 4 |
| `EnumPersistenceTests` | 2 |
| `ConstraintTests` | 5 |
| `SchemaPersistenceTests` | 2 |
| `FoundationSecurityTests` | 1 |
| `DeleteBehaviorTests` | 2 |
| Total | **31** |

Real persisted API workflows still require a selected SQL Server test environment: registration/session persistence and actual refresh rotation/invalidation; ownership query translation; transactional writes and rollback; relational constraints/audit effects. The earlier approved terms-version index deployment remains pending and unchanged. No HTTP/session double closes these database gaps. They do not block the authorized Part 8 source implementation.

## Definition of Done and deployment inputs

| Part 8 item | Evidence / status |
|---|---|
| Every approved endpoint wired to MediatR | 101 requests/actions checked and exercised through HTTP. |
| Thin controllers without business logic | Explicit DTO-to-request mapping, dispatch and response adaptation; no controller database access. |
| Authentication/authorization work | Real JWT/cookie pipeline, policy matrix, SuperAdmin and live-account tests passed. |
| Ownership enforced | Existing handlers retained; HTTP read/write IDOR rejection and customer-list scoping passed with query doubles; real-SQL verification pending. |
| API hardening controls exist | Configuration validation, exact CORS, CSRF, body/upload/rate limits, proxy/host/HTTPS/header/log/error controls and safe exposure tested. |
| API integration tests green | 358 passed. Database limitations above remain explicit. |
| Further concurrency hardening remains outside this Part | Only existing exception-to-HTTP mapping was added; business concurrency logic/schema unchanged. |

No additional source decision or approval is required for the implemented scope. Before running the API, supply the required measured rate/request/depth budgets and existing secret/storage configuration. Before scaled production use, configure the shared protected antiforgery Data Protection key ring and the actual trusted proxy/edge/shared-storage topology. These deployment values are documented inputs, not invented defaults or completed deployment work.

## Follow-up: Development Swagger UI repair

Applied the owner's approved fix for a blank Swagger page. `Program.cs` now registers Swagger/UI once inside the Development condition, retains `/index.html`, and logs that development documentation is available. `SecurityHeadersMiddleware` restores the strict CSP for API/Production responses and limits the scripts/styles/connect exception to Development documentation paths. All other security headers remain active.

Added `SwaggerExposureTests` and allowed the existing test factory to select its synthetic host environment. Nine focused tests passed for Development HTML/assets/generated schema, the CSP boundary, and Production documentation rejection. The entire API regression suite then passed: **367 passed, 0 failed**. The API dependency scan reported no known vulnerable packages from the configured sources.

The running API process locked its normal build output, so verification used an isolated `TestResults/SwaggerFixBuild` output without stopping that process. Commands:

```powershell
dotnet test tests/Brouj.API.IntegrationTests --no-restore -m:1 -p:BaseOutputPath=F:/Mazen/Projects/Brouj/TestResults/SwaggerFixBuild/ -clp:ErrorsOnly --filter FullyQualifiedName~SwaggerExposureTests --logger 'trx;LogFileName=swagger-fix.trx' --logger 'console;verbosity=quiet'
dotnet test tests/Brouj.API.IntegrationTests --no-build --no-restore -m:1 -p:BaseOutputPath=F:/Mazen/Projects/Brouj/TestResults/SwaggerFixBuild/ -clp:ErrorsOnly --logger 'trx;LogFileName=swagger-fix-api.trx' --logger 'console;verbosity=quiet'
dotnet list src/Brouj.API/Brouj.API.csproj package --vulnerable --include-transitive --no-restore
```

No SQL Server or containers were used. Existing database verification remains pending. The running local application needs to be stopped and started again to load the fix, followed by a browser hard reload. HTTP content/assets/schema and headers were verified through WebApplicationFactory; interactive browser rendering was not automated.
