# Part 7 interface-based authorization

Applied the owner's approved `IRequireAccess` design. Authorization is enforced by the registered MediatR pipeline rather than repeated inside each handler.

## Production changes

- [IRequireAccess](../../src/Brouj.Application/Abstractions/Authentication/IRequireAccess.cs) declares a read-only `RequiredAccess` property.
- [AccessLevel](../../src/Brouj.Application/Abstractions/Authentication/AccessLevel.cs) defines Customer, Staff, SuperAdmin and Member. Member allows an active customer or active staff.
- [AuthorizationBehavior](../../src/Brouj.Application/Behaviors/AuthorizationBehavior.cs) performs the declared check before validation, committed effects and transactions. Unknown access requirements fail closed.
- [FeatureAccess](../../src/Brouj.Application/Common/Services/FeatureAccess.cs) remains the centralized current-account checker. It checks active state and compares database staff roles with authenticated claims. Its individual checks are private; callers use `CheckAsync`.
- [ResultFailureFactory](../../src/Brouj.Application/Common/Utils/ResultFailureFactory.cs) preserves the existing typed `Result`/`Result<T>` failure contract when a request is rejected before its handler. It lazily caches only a typed failure-construction delegate; authorization decisions are never cached.
- [Application dependency injection](../../src/Brouj.Application/DependencyInjection.cs) registers authorization after sanitized failure handling and before input validation.

All 84 protected request classes now explicitly implement `IRequireAccess`: 68 Staff, 8 Customer, 6 SuperAdmin and 2 Member. The remaining 17 public/session operations do not implement it. Each requirement is a computed explicit-interface property with no setter or `init`, so it is not part of public JSON request properties.

All 84 former handler guards and their `FeatureAccess` dependencies were removed. Handlers that need trusted identity now inject `ICurrentUser` and keep customer ownership filters and staff self-management safeguards. Resource existence, pricing, reservation/order rules, files and cache intent are unchanged. Each CQRS operation still contains exactly its request, validator and handler files.

Dispatch protected requests through MediatR: directly invoking a handler bypasses pipeline authorization, validation and transaction behavior. No JWT-authentication hook or controller implementation was added. Controller policies can supplement this Application boundary when the API layer is implemented.

## Query and response behavior

A valid protected dispatch performs one account permission lookup. Wrong-realm or anonymous actors can be rejected before a query; unknown access requirements are rejected without a query. Business queries needed by the handler are separate from this count. Public catalogue reads do not perform account permission lookups, even when an actor is present.

Account state is checked again on each protected dispatch, so deleted accounts and stale staff-role claims continue to be rejected. Permission decisions are not reused across dispatches. Authorization rejection preserves the previous error codes and `Result<T>` shapes, and now happens before input validation and transaction entry. Cancellation is checked before authorization and again before advancing to the remaining pipeline.

## Verification

- Solution build: zero warnings and errors.
- Full suite: **923 passed, 0 failed** (24 Domain, 518 Application, 375 Infrastructure, 6 API).
- SQL Server: **31 skipped/blocked**, following the owner's existing instruction to continue with available checks.
- Dependency scan: no known vulnerable packages reported by configured sources; no packages were added.
- Application tests now dispatch through the actual registered MediatR pipeline. Transaction doubles record invocation only and do not claim to prove relational rollback.
- Regression coverage includes the 84 protected requests, the exact access-level contract, the 16 actor/access combinations, public-query bypass, stale/deleted account checks, query counts, JSON permission tampering, cancellation and typed authorization failures.
- Existing conditional SQL feature tests now exercise authorization and validation before their real transaction boundary. They remain unexecuted without SQL Server.

Current logs are the ignored `TestResults/access-refactor.trx` files under the four test projects. No schema or production database change was made by this refactor, and the owner's unrelated global-rule/cache edits were preserved.
