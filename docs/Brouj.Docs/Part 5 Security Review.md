# Part 5 security review

Available foundation hardening and verification are complete. **The security gate remains pending real SQL Server verification.** The owner explicitly selected option 3: continue with available checks and report SQL verification as blocked. No later Part was inspected or implemented.

## PASS items

| Area | Evidence and limits |
|---|---|
| Access-control design | `ICurrentUser` supplies the Application actor. Validated claims distinguish customer/staff identities; Customer, Staff and SuperAdmin policies separate those privileges. Session creation takes IDs from the verified login; rotation/logout derive ownership from the credential. No business request handlers or public business request DTO mappings exist yet, so resource ownership and over-posting enforcement remain deferred. |
| Passwords | Trusted Argon2id, random salts, bounded stored cost parameters, verification/rehash checks and library constant-time digest verification. Temporary password bytes are cleared. Login enumeration and request-level abuse controls require login endpoints. |
| JWT | HS256 signature, exact issuer/audience, JWT type, expiry/skew, required metadata and actor/role validation. Direct and host handlers reject oversized tokens at the shared 8 KiB limit. Failed host validation is reduced to a generic exception before JwtBearer logging. |
| Refresh design | Random opaque credentials; SHA-256 digests in storage; existing SQL Server actor locks, transactions, conditional consumption, unique indexes and stable retry IDs. Code rejects inactive/expired/replayed credentials. Actual database replay, concurrency, rollback and uncertain-commit behavior remain blocked. |
| Injection review | The only raw SQL statements are the two fixed actor-lock queries in `RefreshTokenService.LockActorAsync`. Each uses `FromSqlInterpolated` with a typed GUID. Table names/lock hints are fixed; no input is concatenated into SQL identifiers or commands. Other queries use EF/LINQ. No dynamic sorting/filtering or shell execution exists. |
| Sensitive logging | Application logs contain request type/timing, with no request/response/exception values. Auditing excludes free-form strings and records structured scalar values/property names. EF failure/retry events that can carry provider exceptions are suppressed. Tests inspect rendered messages, structured values and exception chains. |
| Files | Existing generated flat filenames, path normalization, traversal/reparse-point checks, type/MIME/signature allowlists and streamed size checks remain in place. Added tests prove non-seekable overflow, smaller configured limits and cancellation after writing remove partial files. Caller owns the stream. |
| Serialization | Existing cache JSON has bounded depth/size and sensitive-field checks, with actor/role isolation. No dangerous polymorphic type configuration, XML formatter, public entity-binding endpoint or business request DTO mapping was found. |
| Configuration | Strongly typed settings; signing secret and connection string supplied externally. Reviewed source/configuration and a targeted tracked-file secret-pattern scan found no production secret. Pattern matches were code expressions or generated synthetic test keys. The dependency vulnerability scan reported no known vulnerable packages. |

## FAIL items found and corrected

| Finding | Correction |
|---|---|
| EF suppressed save/command errors but could still log exception details through query, bulk-write, retry and transaction failure events. | Expanded the existing suppression policy. Available tests exercise real EF query/update/save failure logging before any connection opens and a real retry strategy with a synthetic timeout. |
| Host authentication retained library validation exceptions before logging and used a larger token limit than the direct service. | Added `RedactingJwtTokenHandler`, delegating validation to Microsoft's handler and replacing failed results before JwtBearer can log them. Shared the direct service's 8 KiB limit; disabled failed-token retention and token-ID logging. |
| Default audience validation treated a trailing slash as equivalent. | Required exact configured audience matching in the shared validation parameters. |
| Staff session creation promised a non-null result although its implementation can reject the actor. | Made `IRefreshTokenService.CreateUserAsync` nullable to match existing failure behavior and remove compiler warnings. |

No unresolved Critical/High issue was identified in the reviewed foundation code. This is a source-review conclusion with the database verification limitation below; it is not an unconditional security approval.

## BLOCKERS and approval items

- Real SQL Server was not used. Docker is installed but its engine is stopped; neither `BROUJ_RUN_SQLSERVER_TESTS` nor `BROUJ_SQLSERVER_TEST_CONNECTION_STRING` is configured. Per the owner's answer, no Docker startup, SQL instance selection or environment installation was attempted.
- All 27 SQL test methods are explicitly skipped, including the added theory for both raw actor-lock paths. Skipped theories expand their cases only when enabled. Refresh replay/rotation/invalidation, concurrent session handling, database constraints, audit persistence and SQL execution therefore remain unverified in this environment. No EF InMemory substitute was used.
- No schema change or additional business decision is needed for these fixes. Owner review of this Part and closure or explicit acceptance of the SQL verification blocker remain necessary for a gate decision. No subsequent Part was started.

## Code changes made

New and edited declarations follow the supplied `IJwtTokenService.cs` style: file-scoped namespaces, separated members and multiline parameter lists. Comments explain security decisions and test boundaries.

| File | Change |
|---|---|
| `src/Brouj.Application/Abstractions/Authentication/IRefreshTokenService.cs` | Correct staff-session result nullability and expand signatures for review. |
| `src/Brouj.Infrastructure/Authentication/JwtTokenService.cs` | Shared size limit, exact audience matching, diagnostic-retention settings and safe malformed-key handling; readable declarations. Existing owner edits were preserved. |
| `src/Brouj.Infrastructure/Authentication/RedactingJwtTokenHandler.cs` | New trusted-handler adapter that removes failed-token diagnostics before HTTP authentication logging. |
| `src/Brouj.Infrastructure/DependencyInjection.cs` | Register the redacting handler and expand sensitive EF failure-event suppression. |
| `tests/Brouj.Application.UnitTests/Behaviors/LoggingBehaviorTests.cs` | Verify response, failure and cancellation secrets do not reach rendered/structured logs or exception attachments. |
| `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/SecurityLogProvider.cs` | Capture rendered and structured log values plus exception chains for security assertions. |
| `tests/Brouj.Infrastructure.IntegrationTests/Services/JwtSecurityTests.cs` | Exercise invalid bearer/cookie credentials and generic failure/log handling; check explicit Authorization precedence. |
| `tests/Brouj.Infrastructure.IntegrationTests/Services/PersistenceLoggingSecurityTests.cs` | Verify query/update/save failures and retry diagnostics without opening a database connection. |
| `tests/Brouj.Infrastructure.IntegrationTests/Services/FileStorageTests.cs` | Add traversal/alternate-stream, control-character/type, streamed size and mid-write cancellation regressions. |
| `tests/Brouj.Infrastructure.IntegrationTests/Services/RefreshTokenInputTests.cs` | Reject SQL-injection-shaped and log-injection-shaped credentials before accessing SQL. |
| `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/FoundationSecurityTests.cs` | Add pending real-SQL tests that inspect actual GUID parameters on both actor-lock paths and reject injection credentials without invalidating the legitimate session. |
| `docs/Brouj.Docs/Part 5 Security Review.md` | Findings, verification, blockers, Definition of Done and deferred checks. |

No entity, approved mapping, migration, package reference, project structure, business feature or API endpoint was changed. No commit or PR was created.

## Tests added/run

Final command:

```powershell
dotnet test Brouj.slnx --configuration Release --no-restore --logger 'trx;LogFilePrefix=Part5Final'
```

| Project | Passed | Failed | Skipped SQL methods |
|---|---:|---:|---:|
| Domain.UnitTests | 24 | 0 | 0 |
| Application.UnitTests | 26 | 0 | 0 |
| Infrastructure.IntegrationTests | 366 | 0 | 27 |
| API.IntegrationTests (existing tests) | 6 | 0 | 0 |
| **Total** | **422** | **0** | **27** |

The baseline was 382 passing cases and 26 skipped SQL methods. This Part adds 40 executable security cases and one real-SQL theory with two pending actor-type cases. TRX files are under each project's ignored `TestResults` directory with the `Part5Final` prefix.

Existing tests also cover Argon2 costs/verification, wrong JWT key/algorithm/claims, actor policies, secure cookies/CSRF, audit redaction and cache isolation. New host JWT cases cover tampering, expiry, wrong issuer/audience, audience-slash mismatch, invalid type, oversized tokens, missing expiry/not-before and customer role injection through both bearer and cookie transport. The existing API test suite was rerun; no new API-only tests were introduced.

Additional checks: Release solution build; `git diff --check`; targeted secret/configuration review; `dotnet list Brouj.slnx package --vulnerable --include-transitive` (no known vulnerable packages from the configured feeds). The dependency graph did not change.

## Definition of Done

| Required item | Result |
|---|---|
| No known Critical/High foundation issue remains | PASS for the reviewed source; database verification limitation explicitly retained. |
| Authentication foundation is safe | PASS for password/JWT/actor/cookie checks; refresh database behavior BLOCKED. |
| Persistence safe from obvious injection mistakes | PASS source review and malformed credential rejection; actual raw SQL parameter/execution tests BLOCKED. |
| File-storage foundation hardened | PASS available traversal/type/size/cleanup tests. |
| Sensitive logging blocked | PASS reviewed foundation logging and available redaction tests; HTTP/deployment logging still needs endpoint-level verification. |
| Security test foundation green | PASS available suite, 422/422; full gate BLOCKED by 27 skipped SQL methods. |
| Deferred API/business checks explicitly listed for Part 10 | PASS, checklist below. |

## Deferred checks for Part 10

- Test IDOR/BOLA on each customer-owned resource, cross-customer access, staff/customer separation and admin/SuperAdmin policy enforcement. Derive identity/role/ownership from the authenticated actor.
- Verify every public request DTO and mapping excludes server-controlled ownership, role, price/total, protected status and audit timestamps. Verify entity binding cannot occur.
- Verify generic login/refresh errors, automated-auth controls, endpoint rate limits, request/multipart size limits, JSON nesting/collection bounds, pagination limits and slow-request controls.
- Test cookie/CSRF behavior on real login, refresh, logout and state-changing endpoints; verify CORS origins/credentials and the deployed topology. Verify authentication-change CSRF token renewal.
- Verify production exception/validation/HTTP/access/proxy/telemetry logging never captures credentials, request bodies, cookies, connection strings, identity numbers or provider exception details. Keep EF sensitive-data logging and IdentityModel PII/security-artifact logging disabled.
- Test security headers, HTTPS, host allowlists, error disclosure, cache behavior, unsafe HTML, redirect handling and proxy/request-smuggling protections at the HTTP/deployment boundary. Add SSRF protections if a feature fetches remote URLs.
- Verify feature-specific file compensation after database failure, storage permissions, safe download headers and shared storage behavior. Existing upload signature checks inspect format markers; full image decoding/re-encoding and malware scanning are not implemented.
- Run the blocked real-SQL injection, refresh replay/expiration/logout/concurrency, uncertain-commit, constraint and audit-persistence tests. Review any newly introduced SQL/sort/filter paths against a whitelist and parameterization rules.

Existing access JWTs remain valid for their approved 15-minute lifetime plus 30-second skew after refresh logout/replacement; refresh invalidation does not provide immediate access-token revocation. This Part retains that documented foundation behavior and adds no revocation schema. Feature code must compensate successful uploads when its subsequent persistence fails and must select explicit non-sensitive cache DTOs; the cache field-name guard cannot infer secrets hidden in arbitrary string values.

## Primary references used during review

- [ASP.NET Core 10.0.11 JwtBearerHandler source](https://github.com/dotnet/aspnetcore/blob/v10.0.11/src/Security/Authentication/JwtBearer/src/JwtBearerHandler.cs): validation failures are logged before the authentication-failed event, which is why redaction belongs in the token handler.
- [Microsoft TokenValidationParameters reference](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters): audience-slash handling, failed-token retention and token-ID logging settings.
- [EF Core connection resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency): retries and uncertain transaction outcomes require database verification; the available logging test verifies only redaction on a retry.
