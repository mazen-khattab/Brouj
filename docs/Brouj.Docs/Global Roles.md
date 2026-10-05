# Brouj Backend — Global Roles

> **Type:** GLOBAL / ALWAYS ACTIVE  
> **Agent visibility:** The agent must read this file before every backend task.  
> **Important:** The agent must not inspect or implement any future Part unless the owner explicitly gives that Part file.

---

# 1. Starting Point

The owner creates the solution, projects, test projects, and project references manually.

Expected existing projects:

```text
src/
├── Brouj.Domain
├── Brouj.Application
├── Brouj.Infrastructure
└── Brouj.API

tests/
├── Brouj.Domain.UnitTests
├── Brouj.Application.UnitTests
├── Brouj.Infrastructure.IntegrationTests
└── Brouj.API.IntegrationTests
```

The agent must **not** create, rename, remove, or restructure these projects unless explicitly asked.

---

# 2. Mandatory Architecture

```text
Clean Architecture
+
CQRS
+
MediatR
+
EF Core
+
SQL Server
```

Rules:

- `Domain` depends on nothing.
- `Application` depends on `Domain`.
- `Infrastructure` depends on `Application + Domain`.
- `API` depends on `Application + Infrastructure`.
- No `ASP.NET Identity`.
- No `GenericRepository<T>`.
- No generic `UnitOfWork`.
- `DbContext` is the effective Unit of Work.
- Application persistence access goes through `IApplicationDbContext`.
- Controllers must be thin.
- No business logic inside Controllers.
- No EF Core / ASP.NET dependencies inside Domain.
- No `HttpContext` inside Application.
- Never return EF entities directly from APIs.

---

# 3. Coding Rules

Every implementation must prioritize:

```text
Correctness
→ Security
→ Performance
→ Clarity
→ Reuse
```

Code must be:

- Clean and idiomatic C#.
- SOLID without over-engineering.
- Async for I/O.
- `CancellationToken` propagated through async operations.
- Free from `.Result`, `.Wait()`, and blocking I/O.
- Free from hidden side effects.
- Easy to test.

## DRY Rule

Do not duplicate business/security rules across the system.

But:

```text
Do not over-DRY.
```

Do not create abstractions only to remove tiny harmless duplication.

---

# 4. Database Rules

- Every entity must have its own `IEntityTypeConfiguration<TEntity>` file.
- Every approved FK, index, unique constraint, delete behavior, converter, and JSON mapping must be defined explicitly.
- Do not invent `MaxLength`, precision, default values, indexes, unique constraints, or check constraints.
- Do not modify the approved schema without approval.
- No lazy loading.
- Read-only queries use `AsNoTracking()`.
- Prefer DTO projection over `Include/ThenInclude`.
- Avoid N+1 queries.
- Pagination is mandatory for large collections.
- SQL must be parameterized.
- Dynamic sort/filter fields must come from a whitelist.

---

# 5. Security Invariants

These rules apply from the **first line of code**, even before the Security Foundation Part is provided.

## Authentication / Sessions

- Never use custom cryptography.
- Passwords must be hashed with a trusted password hasher.
- Use generic login errors; never reveal whether account/email exists.
- JWT signature, issuer, audience, expiry, and expected claims must be validated.
- Access tokens must be short-lived.
- Refresh tokens must support rotation and invalidation.
- Reject replayed/expired/inactive refresh tokens.
- Never log passwords, hashes, JWTs, refresh tokens, cookies, secrets, or full identity numbers.
- Use `HttpOnly` cookies.
- Use `Secure` cookies in production.
- Authentication-sensitive comparisons must use constant-time comparison where applicable.

## Authorization / IDOR / BOLA

- Never trust `UserId`, `CustomerId`, `Role`, ownership, price, or protected status from request bodies.
- Current actor identity comes only from trusted authenticated claims.
- Every customer-owned resource must enforce ownership.
- Every admin/staff action must enforce the correct policy.
- An ID existing in the database never means the caller is authorized to access it.
- Prevent horizontal and vertical privilege escalation.
- Prevent mass assignment / over-posting through explicit request DTOs.

## Injection

Prevent:

```text
SQL Injection
Command Injection
Expression Injection
Header / CRLF Injection
Log Injection
```

Rules:

- EF/LINQ parameterization by default.
- Raw SQL only when required and always parameterized.
- Never concatenate user input into SQL.
- Never execute shell commands from untrusted input.
- Structured logging only; user input must not become the log-template string.

## Browser / API Security

Consider and protect against:

```text
XSS
CSRF
CORS Misconfiguration
Open Redirect
Clickjacking
Host Header Injection
Cache Poisoning
Cache Deception
HTTP Request Smuggling
Information Disclosure
```

Rules:

- API must not generate unsafe HTML from untrusted content.
- CORS with credentials must never use wildcard origins.
- CORS is not CSRF protection.
- Cookie-authenticated state-changing requests must use an approved CSRF strategy when topology requires it.
- Production errors never expose stack traces, SQL, connection strings, or internal paths.
- Security headers must be configured where applicable.

## SSRF

If any feature fetches remote URLs:

- Allowlist destinations where possible.
- Block private/internal network targets.
- Validate redirects.
- Use timeouts.
- Limit response sizes.
- Never fetch arbitrary user-controlled URLs without SSRF protection.

## File Upload Security

- Generate server-side filenames.
- Never trust original filenames as paths.
- Prevent path traversal.
- Enforce size limits.
- Allowlist supported file types.
- Validate content signature when applicable.
- Store uploads outside executable application paths.
- Clean orphan files after failed operations.

## Serialization / Payload Abuse

- Explicit DTOs only.
- Limit request sizes.
- Avoid dangerous polymorphic deserialization.
- Keep XML disabled unless explicitly required.
- If XML is ever enabled, external entities must stay disabled.
- Bound nested/large payloads.

## DoS / Resource Exhaustion

Protect against:

```text
DoS / DDoS
Large Payload Abuse
Slow Requests
Regex DoS
Pagination Abuse
Expensive Query Abuse
Automated Auth Abuse
Automated Reservation Abuse
```

Rules:

- Endpoint-specific rate limits.
- Request/body size limits.
- Maximum page size.
- Bounded work.
- External calls need timeouts.
- Avoid catastrophic regex.
- App-level rate limiting is not a replacement for CDN/WAF/DDoS protection.

## Business Logic / Race Conditions

Protect against:

```text
Double Submit
Duplicate Reservation
Concurrent Order Creation
Price Tampering
Status Tampering
Replay
Race Conditions
```

Rules:

- Price/total/status values are server controlled.
- Use database uniqueness where appropriate.
- Use transactions for atomic operations.
- Use SQL locking/isolation where required.
- Never use in-memory locks for correctness across horizontally scaled instances.

## Supply Chain / Configuration

- No secrets in source control.
- Secrets come from environment/secret stores.
- Dependency vulnerability scanning is required.
- Avoid unnecessary packages.
- Runtime DB credentials should follow least privilege.
- Production debug mode must be off.

---

# 6. Performance Invariants

Target design goal:

```text
~9,000–10,000 RPS
```

This is a **benchmark target**, never a guarantee from code alone.

Rules:

- Backend must be stateless.
- Must support horizontal scaling.
- No correctness dependency on static mutable state.
- No in-process session for distributed correctness.
- No local locks for cross-instance business rules.
- Public hot reads should be cacheable where safe.
- Distributed caching must be possible.
- Keep transactions short.
- Avoid over-fetching.
- Keep payloads small.
- Bound pagination.
- Do not add indexes blindly.
- Optimize only after measurement.
- No security control may be disabled to improve benchmark numbers.

---

# 7. Testing Rules

Every BUILD Part must include tests for what it adds.

Use:

```text
Domain Unit Tests
Application Unit Tests
Infrastructure Integration Tests
API Integration Tests
Security Tests
Concurrency Tests
Performance Tests
```

Critical database behavior must be tested against real SQL Server using `Testcontainers`; do not rely on EF InMemory for relational constraints/locking behavior.

Do not write meaningless tests for DTO getters/setters.

---

# 8. Agent Execution Rules

For every task:

1. Read this file first.
2. Read only the Part file explicitly provided by the owner.
3. Implement only that Part.
4. Do not inspect or start future Parts.
5. Run the tests required by that Part.
6. Check every `Definition of Done`.
7. Report:
   - files added/changed,
   - tests executed,
   - unresolved blockers,
   - anything requiring approval.
8. Stop after the current Part is complete.

If a required decision is missing:

```text
Do not invent behavior.
Do not change schema.
Do not add speculative abstractions.
Do not bypass security.
Do not silently choose a business rule.
```

Implement only work that does not depend on the missing decision.

---

# 9. Mandatory Sequential Execution

| Order | File | Type |
|---:|---|---|
| 1 | `01_PART_1_DOMAIN_LAYER.md` | BUILD |
| 2 | `02_PART_2_APPLICATION_FOUNDATION.md` | BUILD |
| 3 | `03_PART_3_INFRASTRUCTURE_PERSISTENCE.md` | BUILD |
| 4 | `04_PART_4_INFRASTRUCTURE_SERVICES.md` | BUILD |
| 5 | `05_PART_5_SECURITY_FOUNDATION.md` | REVIEW / GATE |
| 6 | `06_PART_6_PERFORMANCE_FOUNDATION.md` | REVIEW / GATE |
| 7 | `07_PART_7_APPLICATION_FEATURES.md` | BUILD |
| 8 | `08_PART_8_API_LAYER.md` | BUILD |
| 9 | `09_PART_9_TRANSACTIONS_CONCURRENCY.md` | BUILD / HARDENING |
| 10 | `10_PART_10_SECURITY_VERIFICATION.md` | REVIEW / GATE |
| 11 | `11_PART_11_PERFORMANCE_VERIFICATION.md` | REVIEW / GATE |
| 12 | `12_PART_12_FINAL_VERIFICATION.md` | REVIEW / GATE |

No Part may begin before the previous Part is approved by the owner.
