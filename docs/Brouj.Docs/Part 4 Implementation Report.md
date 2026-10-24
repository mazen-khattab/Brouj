# Part 4 implementation report

Part 4 implements the approved Infrastructure services and the minimal refresh-token Application contract. Application remains independent of Infrastructure and HTTP. No entity, database column, migration, or mapping configuration was added or changed by this implementation. The owner's updated refresh-token configurations were preserved; the verification fixtures now reflect those approved unique token and filtered active-session indexes.

## Components and behavior

| Component | Implementation |
|---|---|
| Passwords | Isopoh Argon2id v19, 19,456 KiB memory, 2 iterations, 1 lane/thread; 16 random salt bytes, 32-byte digest; verification and rehash detection. |
| JWT | HS256 with a Base64 key containing at least 32 random bytes; issuer/audience required; 15 minutes and 30-second skew; required `sub`, `actor_type`, applicable `role`, `jti`, `iat`, `nbf`, `exp`. |
| Actor access | CurrentUser exposes validated authenticated claims. Customer, Staff, and SuperAdmin authorization policies segregate actor types and roles. |
| Refresh sessions | Existing customer/user tables, 32 random token bytes, SHA-256 hexadecimal digests only in storage, 7-day absolute session deadline, one active refresh credential per actor. |
| Browser cookies | Opt-in JWT cookie reader, host-only HttpOnly/Secure cookies, Strict SameSite by default, framework antiforgery cookie/request-token pair and CSRF middleware. |
| Files | Configurable absolute root outside executable/web directories, generated flat relative names, JPEG/PNG/WebP metadata/signatures, streamed size checks up to 5 MiB, safe deletion and failed-write cleanup. |
| Cache | IDistributedCache adapter, deterministic actor/role scopes, JSON DTOs, five-minute default TTL, explicit same-scope removal, sensitive-field guard and bounded payloads. |
| Transactions | Scoped ApplicationTransactionManager implements the existing MediatR transaction boundary; nested operations join the current transaction. |

Public entry points have XML documentation. Comments explain security decisions inside the implementation: bounded hash parsing, zeroing temporary password bytes, token-digest storage, actor locking, affected-row checks, retry recovery, rollback, upload cleanup, and cache scope isolation.

### Refresh transaction details

Each call creates isolated contexts through the scoped DbContext factory. The SQL Server execution strategy retries the complete refresh transaction using a new context per attempt. A parameterized `UPDLOCK, HOLDLOCK` query locks the actor's parent row, serializing that actor's login, rotation and logout across instances. The existing unique filtered indexes remain the database's final guarantee.

Rotation conditionally deactivates the presented token using its ID, digest, active flag and expiration; exactly one affected row is required before inserting the replacement. The replacement retains the original expiration. An expired credential is rejected and deactivated. A reused/inactive credential returns a generic failure and never revokes another credential. Logout deactivates only the presented active credential, so logout with a stale token cannot revoke a replacement.

New login revokes the prior active credential and inserts its replacement within the same transaction. Duplicate-token/active-index insertion failure rolls back those changes and returns failure. Other database failures surface a generic operational exception without provider error bodies or digest values. Stable replacement IDs/digests across execution-strategy retries allow a successful-but-unacknowledged commit to be recognized rather than repeated. A subsequent session replacement prevents that retry from returning an inactive credential.

Create methods must be called only after a successful verified login. IDs/roles must come from that trusted authentication flow. Rotate/logout derive ownership from the presented credential and do not take a caller-supplied actor ID. Customer credentials cannot be used against the staff table or vice versa. Soft-deleted actors cannot receive new credentials.

The general Application transaction manager intentionally does not replay arbitrary MediatR handlers. It uses a non-retrying execution boundary, rolls back on failure, and clears stale tracking. Refresh operations have their own specifically replay-safe transaction implementation.

## Configuration and host integration

Register persistence and services in the host:

```csharp
services.AddInfrastructure(connectionString);
services.AddInfrastructureServices(configuration);
```

Keep `MultipleActiveResultSets=False` in the SQL Server connection configuration to retain the existing audited-save transaction/savepoint guarantees.

| Configuration key | Value/purpose |
|---|---|
| `Jwt:Issuer` | Required issuer string. |
| `Jwt:Audience` | Required audience string. |
| `Jwt:SigningKey` | Required Base64 secret from an environment variable/secret store; at least 32 random bytes. |
| `Jwt:AccessTokenLifetimeMinutes` | Defaults to 15; the approved value is enforced. |
| `Jwt:RefreshTokenLifetimeDays` | Defaults to 7; the approved value is enforced. |
| `FileStorage:RootPath` | Required absolute writable local directory outside `wwwroot` and the executable directory. |
| `FileStorage:MaxFileSizeBytes` | Defaults to 5,242,880; may be reduced, never increased beyond 5 MiB. |
| `FileStorage:AllowedExtensions` | Defaults to `.jpg`, `.jpeg`, `.png`, `.webp`; may restrict the approved formats. |
| `FileStorage:AllowedContentTypes` | Defaults to `image/jpeg`, `image/png`, `image/webp`. |
| `Caching:Provider` | `Memory` for development (default), or `External` to use a host-registered IDistributedCache. |
| `Caching:DefaultExpirationSeconds` | Defaults to 300; positive, at most one day. |
| `AuthenticationCookies:AccessTokenCookieName` | Defaults to `__Host-brouj-access`. |
| `AuthenticationCookies:RefreshTokenCookieName` | Defaults to `__Host-brouj-refresh`. |
| `AuthenticationCookies:SameSite` | Defaults to `Strict`; validated values Strict/Lax/None. Secure remains mandatory. |
| `AuthenticationCookies:Path` | `/`, required by the host-cookie prefix. |

For standard .NET environment binding, use names such as `Jwt__SigningKey` and `FileStorage__RootPath`. Generate the signing key with `RandomNumberGenerator.GetBytes(32)` and Base64-encode it in a trusted provisioning process. Never commit the generated value, connection credentials, or real authentication tokens. No deployment secrets were written by this implementation.

### Browser authentication

Cookie JWT consumption is opt-in. Register `services.AddBroujCookieAuthentication()` and use this order in the browser host pipeline:

```csharp
app.UseAuthentication();
app.UseBroujCookieProtection();
app.UseAuthorization();
// Host endpoints follow.
```

CookieCsrfMiddleware validates every unsafe request in that browser pipeline, including unauthenticated login and refresh/logout when the access cookie has expired. A missing or invalid antiforgery token returns HTTP 400 before the handler executes. GET/HEAD/OPTIONS must remain side-effect-free. A bearer-only host can omit cookie consumption and this browser middleware; use distinct pipelines if browser and non-browser clients need different CSRF handling.

Use AuthenticationCookies.GetCsrfToken to create the framework cookie/request-token pair in an HTTPS, no-store bootstrap response. The browser sends the returned request token in `X-CSRF-TOKEN` for unsafe operations. Retrieve a fresh request token after login/logout or when expiration changes the authenticated identity. The antiforgery cookie is HttpOnly/Secure and shares the configured SameSite mode; the request token is returned separately rather than read from that cookie. Shared deployments must configure a shared, protected ASP.NET Data Protection key ring so antiforgery tokens survive instance changes.

AuthenticationCookies.Write/Clear apply identical host/path/security settings and no-store response headers. An explicit Authorization header takes precedence over an access cookie. Cookie transport requires HTTPS, including local testing. This part provides services and middleware; API endpoints, bootstrap routes, handlers and host pipeline composition are not introduced here.

### Cache and file usage

Logical cache keys are bounded `resource:key` namespaces using ASCII letters/digits, colon, dash, underscore or period (maximum 200 characters). Keys must contain no secrets. Physical keys hash namespace plus anonymous/customer/staff-and-role scope, prefixed `brouj:v1:`. Authenticated and anonymous cache entries never share a scope. RemoveAsync invalidates the caller's same scope; callers must remove affected keys after successful writes and choose resource keys consistently with DTO types. Entries for other scopes expire at their TTL. Cross-actor invalidation/business-specific resource caching belongs in the relevant feature's explicit design. Authorization and session correctness always use the authoritative database/validated principal.

Cache only explicit non-sensitive DTOs. The JSON field-name guard rejects obvious password/token/cookie/secret/identity-number/signing-key fields recursively, but cannot infer every sensitive meaning from arbitrary names or string contents; callers remain responsible for selecting safe DTOs. Payloads are limited to 1 MiB and depth 16. Corrupt/disallowed entries become a miss and are removed. Development Memory is instance-local; select External and register a distributed provider for shared cache hits across deployments. Redis is compatible through IDistributedCache, but no Redis dependency, server, or cloud service was installed.

File references contain only a random GUID filename and canonical extension; original upload names never become paths. Both actual and declared length must match, and the actual stream is bounded even when it cannot seek. The caller retains ownership of the input stream. Root ancestors and stored delete targets reject reparse points/junctions. Give the service account exclusive write/delete access to that root: application path checks cannot defeat a privileged local process changing filesystem links concurrently. File.Delete and directory metadata checks use the operating system's synchronous metadata APIs; stream reads/writes are asynchronous and propagate cancellation.

When a future caller uploads before persisting its database reference, it must compensate a failed database operation by calling DeleteAsync with the returned reference and a cleanup token that has not already been cancelled. Internal streaming/flush/read failures and cancellation remove incomplete files, including failures after the declared number of bytes was written. Signature checks validate the approved format markers; this part does not decode/re-encode images or perform malware scanning. Local storage requires an appropriately shared/mounted directory if multiple hosts must access the same uploads.

### Operational limits

Access JWTs are stateless: login replacement/logout revoke refresh credentials; previously issued access JWTs remain valid until their 15-minute lifetime plus allowed skew ends. No new JWT revocation schema was introduced. SHA-256 hashes are the only refresh-token representation in the tables. Existing pre-service plaintext tokens, if present in a deployed database, will not match the new hashed lookup and require a fresh login; no production data migration was performed.

Never enable EF sensitive-data logging or IdentityModel PII/security-artifact logging. EF SaveChangesFailed/CommandError diagnostic logging is suppressed at registration because provider error bodies can include credential digest values. Service failures expose generic errors. Host logging must likewise exclude credentials and avoid destructuring RefreshTokenResult (its string representation is redacted, its actual credential properties are necessary for client issuance).

## Verification

Release build succeeded with zero warnings/errors. `dotnet test Brouj.slnx --configuration Release --no-restore` passed the full available suite; the final Infrastructure run after upload-close and authenticated antiforgery verification also passed:

| Project | Passed | Pending SQL methods reported skipped |
|---|---:|---:|
| Domain.UnitTests | 24 | 0 |
| Application.UnitTests | 24 | 0 |
| Infrastructure.IntegrationTests | 328 | 26 |
| API.IntegrationTests | 0 (existing empty project) | 0 |
| Total | 376 | 26 |

Skipped theories are reported as methods before case expansion; the real-SQL run will enumerate their cases. The API test project currently has no tests; no API functionality was added. Part 4 is implementation-complete with all available verification passing; SQL Server integration/concurrency/retry verification remains explicitly pending.

Available tests exercise approved Argon2 costs/salt uniqueness, verification/rehashing/malformed hashes, minimal JWT claims, lifetime/skew, tampering, issuer/audience/key, correctly signed invalid actor/role/metadata, actual JWT authentication handler and actor authorization policies, secure cookies/framework CSRF, refresh input/digest validation without SQL, upload metadata/signature/size/path/read-failure cleanup, cache get/set/remove/TTL/corruption/isolation/provider selection, and all prior unit/model/mapping/audit-preparation checks.

SQL Server tests remain **pending/blocked by the current environment**. Docker was not installed and no EF InMemory substitute was used. Added real-SQL tests cover customer/staff rotation, hash persistence, replay, logout, expiration, one active session, concurrent logins/refreshes, soft deletion/table separation, duplicate-token rollback, lost commit acknowledgement recovery, logout-versus-refresh races, and the Application transaction rollback boundary. The test collection remains disabled unless a real test server or compatible container runtime is explicitly configured.

To run pending tests against a real SQL Server, provide `BROUJ_SQLSERVER_TEST_CONNECTION_STRING` and run the Infrastructure tests. The fixture creates/drops only its generated `BroujPart3_<GUID>` test databases; the supplied test server account must be allowed to create/drop those databases. Alternatively, once a compatible container runtime exists, set `BROUJ_RUN_SQLSERVER_TESTS=1` to use the existing SQL Server Testcontainers fixture. Do not point these tests at production.

Dependency scan `dotnet list Brouj.slnx package --vulnerable --include-transitive` completed: no known vulnerable dependencies across all ten projects using the current feeds.

### Release password benchmarks

Executed successfully:

```powershell
dotnet run --project benchmarks/Brouj.PasswordBenchmarks/Brouj.PasswordBenchmarks.csproj --configuration Release --no-restore -- --job short --filter '*' --warmupCount 1 --iterationCount 3
```

BenchmarkDotNet 0.15.8; .NET SDK 10.0.401 / runtime 10.0.12; Windows 11; AMD Ryzen 5 PRO 3400G, 4 physical / 8 logical cores; one launch, one warmup, three measured iterations.

| Method | Mean | 99.9% CI half-width | Standard deviation | Managed allocation/op |
|---|---:|---:|---:|---:|
| Hash | 111.5 ms | 18.93 ms | 1.04 ms | 118.58 MB |
| Verify | 113.0 ms | 28.88 ms | 1.58 ms | 118.58 MB |
| VerifyWrongPassword | 112.4 ms | 94.09 ms | 5.16 ms | 118.58 MB |

These are short machine-specific measurements with wide confidence intervals, not a load test or an RPS guarantee. The managed implementation's cumulative allocations exceed the algorithm's 19 MiB working-memory parameter; allocation/op is not peak resident memory. Security parameters were not reduced. Longer Release runs are supported by omitting the short-job arguments. Raw CSV/HTML/Markdown results are generated in the ignored BenchmarkDotNet.Artifacts/results directory; this table preserves the measured output in documentation.

## Changed files

- `Brouj.slnx`: includes the expressly approved benchmark project.
- `benchmarks/Brouj.PasswordBenchmarks/{Brouj.PasswordBenchmarks.csproj,Program.cs,PasswordBenchmarks.cs}`: runnable BenchmarkDotNet project.
- `src/Brouj.Application/Abstractions/Authentication/IRefreshTokenService.cs`: approved lifecycle contract/result, with redacted string formatting.
- `src/Brouj.Application/Common/Settings/{JwtOptions,FileStorageOptions,CacheOptions,CookieOptions}.cs`: approved security/default values.
- `src/Brouj.Infrastructure/Brouj.Infrastructure.csproj`: trusted Argon2/JWT packages and ASP.NET framework reference.
- `src/Brouj.Infrastructure/DependencyInjection.cs`: persistence factory, options/provider/services, actor policies, cookie/CSRF integration helpers.
- `src/Brouj.Infrastructure/Authentication/{PasswordHasher,JwtTokenService,TokenClaims,CurrentUser,RefreshTokenDigest,RefreshTokenService,AuthenticationCookies,CookieCsrfMiddleware}.cs`.
- `src/Brouj.Infrastructure/Storage/{LocalFileStorageOptions,FileStorageService}.cs`.
- `src/Brouj.Infrastructure/Caching/DistributedCacheService.cs`.
- `src/Brouj.Infrastructure/Persistence/ApplicationTransactionManager.cs`.
- `tests/Brouj.Infrastructure.IntegrationTests/Services/{AuthenticationTests,RefreshTokenInputTests,CookieTests,FileStorageTests,CacheTests}.cs`.
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/RefreshTokenPersistenceTests.cs`: pending real SQL Server lifecycle/concurrency/retry tests.
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/SqlServerFixture.cs`: optional retry/interceptor support for those real SQL tests; existing Part 3 behavior stays unchanged by default.
- `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/ApprovedIndexes.tsv` and `Model/RelationshipTests.cs`: verify the owner's updated refresh indexes without changing their configuration.
- `docs/Brouj.Docs/Part 4 Implementation Report.md`: configuration, host responsibilities, measured results and pending verification.

The owner-modified CustomerRefreshTokenConfiguration.cs and UserRefreshTokenConfiguration.cs were already dirty when this work resumed and were not edited by the implementation. No future Part was inspected or implemented, and no commit or PR was created.

## Reference material

- [Isopoh Argon2 configuration API](https://mheyman.github.io/Isopoh.Cryptography.Argon2/api/Isopoh.Cryptography.Argon2.Argon2Config.html).
- [EF Core connection resiliency and commit uncertainty](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).
- [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
