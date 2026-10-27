# Part 10 — Security verification

Review date: 2026-10-27. Scope: implemented backend, [OWASP Top 10:2025](https://top10.owasp.org/2025/), [OWASP API Security Top 10:2023](https://api-security.owasp.org/editions/2023/en/0x11-t10/), and Global Security Invariants. The owner confirmed the system is **not deployed** and approved opt-in startup provisioning with all administrator fields in local user-secrets. No business feature, entity, approved schema, migration, role definition, or package was added.

Gate status: **PASS for the reviewed undeployed backend**. All 1,452 tests passed with no failures or skips; both High code findings were fixed. The local user-secrets section was populated and verified without printing values. This is a code/test gate; the pending deployment controls below are not represented as verified production evidence.

## Critical

No Critical finding identified in the reviewed code and executed scenarios.

## High

| Finding | Fix and evidence |
|---|---|
| H1: Startup created a privileged account using credentials committed in source. Startup also swallowed failures and logged the original exception. | Removed every literal account field. Added explicit, default-disabled `InitialAdminOptions`, existing staff validation, a configured stable ID and SuperAdmin restriction. Hashing uses the existing trusted hasher. Seeding checks live database state and serializes concurrent startup through SQL locks. Failures stop startup with a sanitized exception and typed logs. `InitialAdminOptionsTests` and real-SQL `Part10BootstrapTests` cover disabled/invalid settings, all configured fields, hashing, restart preservation, concurrent startup and failure redaction. |
| H2: Reservation cancellation could race order creation and overwrite the reservation's confirmed state using a stale Processing read. | Cancellation now participates in a transaction and acquires the same unit lock as order creation before loading mutable state. The initial lookup is scoped to the current customer. SQL `Part10BusinessSecurityTests.Customer_cancellation_and_admin_order_creation_cannot_both_succeed` coordinates independent callers at the lock: exactly one succeeds, with consistent reservation/order state. Existing Part 9 races are rerun. |

No unresolved High code finding remains after these fixes. The obsolete bootstrap password remains in historical commits; the owner confirms there is no deployment using it. It must never be reused, and an existing local account created by the old seeder requires a password change. No Git history rewrite or local database reset was performed.

## Medium

- **Fixed:** Staff-login logs included submitted email addresses, and reported success before checking the session result. Logs now use fixed templates and safe outcome metadata, including actual session success. HTTP tests verify that known/missing/deleted accounts have identical login failures and that emails, passwords, hashes and CRLF/Unicode log-forging payloads are absent from logs.
- **Deployment decision pending:** IP rate limits are per API instance. They provide tested local throttling, including failed login and forwarded-IP spoofing resistance, but distributed credential stuffing and automated business abuse require an aggregate edge/gateway policy. No production threshold, account lockout, CAPTCHA or MFA policy was invented.
- **Existing session design:** Refresh credentials rotate, are stored as digests, and are invalidated on logout/replacement. An issued access JWT can remain valid until its approved 15-minute expiry plus 30-second clock skew. Password changes retain sessions under the previously approved rule. Deleted accounts and changed staff roles are rejected using current database state on protected dispatch. Immediate access-token revocation and revocation on account restore require a separate owner decision; this review adds no session schema.

## Low

- Upload verification checks metadata, streamed size and format signatures; it does not fully decode images or provide malware scanning. Files receive generated names outside executable directories and cannot overwrite existing files. A filename such as `image.exe.png` is safely replaced with an opaque `.png` filename when its signature/MIME agree; an executable final extension is rejected. Public media hosting, if configured later, must preserve correct content types and nosniff.
- Development retains the existing Swagger/CSRF environment behavior. Production applies CSRF validation to unsafe browser requests, including login/refresh/logout. Development must remain local; deployment must use Production with the reviewed middleware path.
- Local user-secrets is not encrypted. No production configuration or actual production SQL login exists to inspect. The API does not expose runtime secrets, raw audit JSON or dependency details through its responses.

## Passed controls and evidence

| Area | Verification |
|---|---|
| IDOR/BOLA, horizontal access | Real-handler HTTP and Application tests deny another customer's reservation read/cancel and order detail/list. Customer order detail remains owner-scoped under the admin URL prefix. No reservation update operation exists; attempted PUT/PATCH returns 405 and cannot change a foreign record. |
| Function-level authorization / forced browsing | Endpoint inventory covers every one of the 101 approved requests. Every protected route is tested with anonymous and wrong-realm callers; current account/role checks apply through Application. SuperAdmin-only management rejects ordinary admins. Hidden GUIDs and owner/role query overrides grant no access. |
| BOPLA / mass assignment | Explicit DTOs, fixed access metadata and rejection of unmapped JSON prevent customer/user/role/status/price/number/type-metadata injection. Order creation derives its reservation from the route, computes price server-side and chooses status server-side even if an extra body attempts overrides. |
| Authentication and enumeration | Trusted Argon2id implementation, random salts, bounded stored hash costs, dummy verification for missing/deleted accounts, generic login failures in both realms, and credential-free logs. Structural hasher tests verify the trusted path; no claim of identical measured network latency is made. Registration retains the approved generic duplicate-details conflict rather than introducing a new registration policy. |
| JWT/session | Host/service tests reject tampering, expiry, wrong issuer/audience/key/algorithm and invalid actor/metadata claims. Real SQL tests cover refresh rotation, replay, expiry, one active session, concurrent refresh/login, rollback, uncertain commit, logout races and deleted actors. Valid access tokens cannot read deleted accounts' protected data. |
| SQL injection | All production raw SQL uses fixed statements with interpolated parameters. SQL tests submit quote, DROP TABLE and wildcard payloads through the real city-search handler, inspect parameterized commands, and verify that data/tables remain intact. Refresh credential injection is rejected before database lookup. |
| Sort/filter, command/header/log injection | No dynamic expression or arbitrary sort API exists. Supplied unsupported sort/filter/returnUrl inputs cannot select executable expressions or redirect targets. No process execution or shell integration exists in application code. Host/proxy controls and malicious upload/login fields are tested. Logs use fixed templates and exclude raw request fields. |
| CORS/CSRF | Credentialed CORS uses exact configured HTTPS origins; disallowed origins receive no allow-origin permission. CSRF is checked against the authenticated identity, including anonymous login and expired-access refresh. Cross-identity proofs fail. |
| Browser/API privacy | Security headers, no-store, fixed host/HTTPS redirect destination, trusted proxy boundaries, generic production errors and disabled production Swagger/OpenAPI are covered by HTTP tests. Stored markup remains JSON data with application/json, nosniff and CSP even when HTML is requested; consuming clients must render it as text. |
| File/payload abuse | Existing and new tests cover traversal, alternate streams, double extensions, malicious/control-character names, wrong signatures, unsupported types, declared/actual size mismatches, streamed overflows, cancellation/cleanup, filename reuse and non-overwrite behavior. JSON size/depth, content type, XML and unexpected type/property payloads are bounded/rejected. |
| Resource exhaustion | Every endpoint has a rate category; page-size limits, bounded nested collections, request/file limits and bounded regex execution are tested/reviewed. Authentication hashing work is trusted and bounded. No remote-fetch integration or unbounded caller-selected network destination exists. |
| Race/business abuse | Full Part 9 concurrency/atomicity suite plus cancellation/order race. HTTP tests cover server-derived price/status, active reservation duplicates and order retry conflicts. Persistent idempotency response storage remains the prior deferred schema proposal. |
| Secrets/dependencies | Production credentials removed from source; redacted current-file scanner passes. Synthetic test credentials are distinguished from live secrets. NuGet vulnerability scan includes transitive packages and reports no known vulnerable packages in configured sources. Historical bootstrap credential exposure is explicitly documented above. |

SSRF, unsafe third-party API consumption and command injection integrations are **not applicable to the current implementation**: source inventory found no remote HTTP fetch or process-execution path. This is not a claim about future integrations.

## OWASP coverage

| OWASP 2025 | Reviewed controls |
|---|---|
| A01 Broken Access Control | Ownership, fixed policies, server-controlled fields; SSRF inventory |
| A02 Security Misconfiguration | Bootstrap, configuration validation, CORS/CSRF, host/proxy, headers, docs exposure |
| A03 Software Supply Chain Failures | Direct/transitive dependency scan and repository secret review |
| A04 Cryptographic Failures | Trusted password hashing, JWT signatures, random refresh credentials and digest storage |
| A05 Injection | SQL parameters, fixed query shape, input/log/header/file boundaries |
| A06 Insecure Design | Business invariants, ownership, bounded work and abuse controls |
| A07 Authentication Failures | Login/session/JWT/deleted-account tests |
| A08 Software or Data Integrity Failures | Explicit DTOs, server-derived totals/status, transaction atomicity |
| A09 Security Logging and Alerting Failures | Sanitized structured logs; deployment alerting/retention is pending |
| A10 Mishandling of Exceptional Conditions | Sanitized startup/provider/API failures, rollback and uncertain commits |

API 2023 coverage: API1/3/5 access control and property/function authorization; API2 authentication/session tests; API4/6 limits, races and business abuse; API7 no remote-fetch path; API8 configuration/browser controls; API9 endpoint inventory, fixed liveness and production documentation exposure; API10 no third-party API consumption path.

## Tests executed

Final runs: **1,452 passed, 0 failed, 0 skipped**.

| Suite | Passed | Evidence |
|---|---:|---|
| API | 398 | `tests/Brouj.API.IntegrationTests/TestResults/part10-api.trx` |
| Application | 528 | `tests/Brouj.Application.UnitTests/TestResults/part10-application.trx` |
| Infrastructure | 502 | `tests/Brouj.Infrastructure.IntegrationTests/TestResults/part10-infrastructure.trx` |
| Domain | 24 | `tests/Brouj.Domain.UnitTests/TestResults/part10-domain.trx` |

The Infrastructure run used `BROUJ_RUN_SQLSERVER_TESTS=1`, Docker and the existing Testcontainers fixture; no supplied application connection string was used. It includes every Part 9 SQL concurrency/atomicity case, all new bootstrap/cancellation/injection cases, and existing authentication/session/security tests. The earlier focused bootstrap run passed 10 tests and the focused Infrastructure security run passed 22 tests.

`git diff --check`, the redacted secret scanner, and `dotnet format whitespace ... --verify-no-changes` scoped to all 16 changed C# files passed. The dependency scan with transitive packages reported no known vulnerable packages. No dependency was added or updated.

The local `InitialAdmin` section contains Enabled, Id, Name, Email, Phone, Role and Password. Name/email/phone were moved from the previous development profile; ID and password were freshly generated, and Role is SuperAdmin. The previous exposed password was not reused. The write verified that all other existing user-secrets settings were preserved. No values were printed, copied into repository files or used to provision an application database during this task.

After updating the local secrets, all nine `SwaggerExposureTests` were rerun successfully to verify Development/Production documentation behavior and HTTP test isolation from the local bootstrap configuration.

Reproduction with Docker Desktop running:

```powershell
$env:BROUJ_RUN_SQLSERVER_TESTS = '1'
dotnet test tests/Brouj.Infrastructure.IntegrationTests/Brouj.Infrastructure.IntegrationTests.csproj
dotnet test tests/Brouj.API.IntegrationTests/Brouj.API.IntegrationTests.csproj
dotnet test tests/Brouj.Application.UnitTests/Brouj.Application.UnitTests.csproj
dotnet test tests/Brouj.Domain.UnitTests/Brouj.Domain.UnitTests.csproj
dotnet list Brouj.slnx package --vulnerable --include-transitive --no-restore
& ./scripts/security/Verify-TrackedSecrets.ps1
```

The secret scanner is a redacted heuristic for current repository text and production/configuration literals. It is not a full Git-history or runtime-secret-store scan.

## Fixes made / changed files

Production:

- `src/Brouj.API/Program.cs`
- `src/Brouj.Infrastructure/DependencyInjection.cs`
- `src/Brouj.Infrastructure/Seeder/UsersSeeding.cs`
- `src/Brouj.Infrastructure/Seeder/InitialAdminOptions.cs` (new)
- `src/Brouj.Application/Features/Users/Commands/Login/LoginUserCommandHandler.cs`
- `src/Brouj.Application/Features/Reservations/Commands/Cancel/CancelReservationCommand.cs`
- `src/Brouj.Application/Features/Reservations/Commands/Cancel/CancelReservationCommandHandler.cs`
- `src/Brouj.Application/Behaviors/FeatureFailureBehavior.cs`

Tests:

- `tests/Brouj.API.IntegrationTests/Support/ApiTestConfiguration.cs`
- `tests/Brouj.API.IntegrationTests/Part10AuthenticationSecurityTests.cs` (new)
- `tests/Brouj.API.IntegrationTests/Part10BusinessSecurityTests.cs` (new)
- `tests/Brouj.Application.UnitTests/Features/Part9ConflictMappingTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Services/InitialAdminOptionsTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/Services/Part10FileAbuseTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part10BootstrapTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part10BusinessSecurityTests.cs` (new)

Supporting files: `scripts/security/Verify-TrackedSecrets.ps1`, `docs/Brouj.Docs/Part10.InitialAdmin.md`, and this report. The local user-secrets update is outside the repository and never appears in this inventory or test output with its values.

## Remaining risks / owner decisions

The owner authorized recording external controls as pre-deployment requirements. The following are **pending deployment verification**, not assumed accepted or tested in production:

- Runtime SQL login restricted to necessary table DML, without sysadmin, db_owner, schema-management or migration permissions; separate migration credentials. SQL grants/current server roles must be reviewed against the actual login before deployment.
- SQL transport encryption with certificate validation, TLS termination, canonical host/origins, explicitly trusted proxy addresses, aggregate edge throttling/WAF and slow-request limits.
- Production secrets, disabled/bootstrap configuration after provisioning, protected shared Data Protection keys where multiple instances serve CSRF requests, and upload directory ownership/ACLs.
- Security-log access, retention and alert routing; frontend text rendering of stored fields; any desired immediate JWT revocation or stronger registration/credential-stuffing policy.

No external service was contacted to provision accounts, no application database was changed, and no repository history was rewritten. Only isolated Testcontainers databases are used for SQL verification. No future Part was inspected or implemented.

## Definition of Done

- [x] No known Critical/High code issue remains in the reviewed undeployed backend; historical credential exposure and external requirements are explicitly recorded.
- [x] IDOR/BOLA, function/property authorization and forced-browsing tests pass.
- [x] Authentication/session/JWT/deleted-account tests pass.
- [x] SQL injection and malicious input/log/header handling tests pass.
- [x] API/browser security controls are verified through the actual HTTP pipeline.
- [x] File/payload abuse tests pass.
- [x] Part 9 races, atomicity, retry behavior and the new cancellation/order race pass against real SQL Server.
- [x] Current production source/config secret review and direct/transitive dependency review pass.

No implementation or test blocker remains. Production SQL privileges, edge controls, secret hosting and the other deployment requirements above remain unverified because the owner confirms no deployment exists. The review makes no production certification or performance claim and stops at Part 10.
