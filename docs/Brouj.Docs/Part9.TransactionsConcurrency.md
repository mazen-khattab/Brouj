# Part 9 — Transactions and concurrency

Implementation date: 2026-10-27. Scope: Part 9 only. SQL verification uses SQL Server 2022 through the existing Testcontainers fixture, as authorized by the owner.

## Resulting behavior

- `CreateOrderFromReservation` starts a transaction before reading eligibility, finds the target unit, obtains a parameterized `UPDLOCK, HOLDLOCK` on its `BaseUnits` row, and then reads reservation state and existing orders. A regular unit rejects **any** existing order through any reservation, regardless of order status, as required by Part 9. Initiative units retain their approved multiple-Pending-order behavior.
- `CompleteInitiativeOrder` uses the same unit lock before loading mutable order state. Winner selection, cancellation of other orders, completion timestamps and staff audit rows commit together. It also serializes with new order creation for that unit.
- `CreateReservation` retains the approved filtered unique index `UQ_Reservations_CustomerId_UnitId_Processing` on `(CustomerId, UnitId) WHERE [Status] = 'processing'`. The pre-check provides an ordinary business rejection; SQL uniqueness closes the race. Cancelled and Confirmed reservations are outside that filter.
- The transaction manager translates only the two approved duplicate constraints, SQL deadlock victims (1205), snapshot update conflicts (3960), and optimistic concurrency exceptions into safe conflict categories after transaction disposal. Application maps the relevant categories to existing feature error codes and HTTP returns 409. Unrelated uniqueness/provider failures remain generic failures.
- All eight specified commands now declare `ITransactionalRequest`. A returned failed business result rolls back earlier saves and clears tracked state. Exceptions also clear tracked state and dispose the uncommitted transaction.
- `NonReplayingExecutionStrategy` establishes EF's ambient execution scope with zero retries. This fixes explicit transactions with the production `EnableRetryOnFailure` configuration without replaying commands or file effects. The old `NonRetryingExecutionStrategy` wrapper did not establish that ambient scope; the real SQL tests exposed this incompatibility.
- Transactions contain database work only. The existing outer `CommittedEffectsBehavior` deletes queued files and emits cache invalidations after successful commit; failures discard pending cleanup. A real deletion test checks that file deletion runs after the transaction ends.
- Structured `ILogger<T>` messages cover transaction start, commit, business rollback, failed work, conflict classification and feature mapping. Provider exceptions and their private values are not passed to those log calls. Infrastructure registers logging for worker/test hosts as well as the API.

No schema, migration, entity, enum database value, package dependency or public endpoint was added or changed. No process-local lock supplies application correctness. Test rendezvous objects only coordinate test timing; separate contexts/connections perform the actual SQL locking.

## Transaction verification

`Part9AtomicityTests` injects failure in `SavedChangesAsync`: real SQL writes have already completed, but the outer command transaction remains open. A fresh context compares parent/child identities, reservation/order states, completion times, cascaded plan rows and audit identities with the baseline. These tests cover:

| Operation | Atomic state checked |
|---|---|
| CreateProject | BaseProject + InitiativeProject + audit rows |
| CreateUnit | BaseUnit + InitiativeUnit + audit rows |
| CreateReservation | New Processing reservation |
| CreateOrderFromReservation | Order + reservation confirmation + audit rows |
| CompleteInitiativeOrder | Winner + cancelled competing orders + timestamps + audit rows |
| CreateTimePlan | Plan + copied stages/items + audit rows |
| AddStageFromTemplate | Complete copied stage/item tree + audit rows |
| DeleteTimePlan | Plan + cascaded stages/items/images + audit rows; files retained on failure |

The existing initiative-project child SQL failure test additionally exercises provider failure before the full save completes. New transaction tests cover rollback after an earlier successful save, a returned business failure, unrelated unique failures, a real deadlock and a lost commit acknowledgement.

## Idempotency assessment and approval item

Current protection is business uniqueness and guarded state transitions. It does not promise persistent replay of the original response for an arbitrary client idempotency key.

| Operation | Repeated request after success | Limits |
|---|---|---|
| CreateReservation | While the reservation is Processing, returns ReservationAlreadyActive / 409 without inserting another row. | After cancellation, another reservation must be allowed. Confirmed is also outside active uniqueness; initiative Pending orders may leave the unit available. Therefore `(CustomerId, UnitId)` is not a permanent operation identity. |
| CreateOrderFromReservation | Returns OrderConflict / 409; the reservation uniqueness constraint and unit lock prevent another order. | The response is a conflict, rather than a replay of the original 201 response and identifier. |

A lost commit acknowledgement is an uncertain outcome. Commands are never automatically replayed. Clients can reconcile using the existing reservation/order reads before deciding whether another request is appropriate. The test simulates an exception after SQL Server commits and proves the delegate ran once and exactly one row persisted.

**Deferred for owner approval:** If persistent response replay is wanted, approve a persistent idempotency design before implementation. Proposed scope is the authenticated actor, operation and client key, with a request fingerprint, completion/resource result and retention policy. The record and business write would commit in the same transaction; reuse of a key with a different fingerprint would be rejected. Key limits, storage representation, retention, replay response and constraints remain owner decisions. No table, index, migration, request header contract or retention behavior has been implemented for this proposal. This deferred subtask does not block the requested database concurrency guarantees.

## Test execution

Final runs on 2026-10-27: **1,404 passed, 0 failed, 0 skipped**.

| Suite | Passed | Evidence |
|---|---:|---|
| Domain | 24 | `tests/Brouj.Domain.UnitTests/TestResults/part9-domain.trx` |
| Application | 527 | `tests/Brouj.Application.UnitTests/TestResults/part9-application.trx` |
| Infrastructure | 480 | `tests/Brouj.Infrastructure.IntegrationTests/TestResults/part9-infrastructure.trx` |
| API | 373 | `tests/Brouj.API.IntegrationTests/TestResults/part9-api.trx` |

The Infrastructure run had `BROUJ_RUN_SQLSERVER_TESTS=1`, no supplied SQL connection string, and a working Docker Linux engine. The existing fixture provisioned and disposed its own SQL Server Testcontainer and isolated databases. No shared application database was used. All 28 Part 9 Infrastructure tests passed: 27 exercise real SQL Server and one checks the transaction guard before issuing SQL. The Part 9 tests include 12 concurrency/retry cases, nine atomicity/file-cleanup cases and seven transaction/conflict cases. Nine new Application mapping cases and six new HTTP retry/conflict cases also passed.

Mandatory results:

- Two different admins and different reservations for one regular unit: exactly one successful order; exactly one order persisted; the losing reservation remains Processing; only the successful operation's audit rows persist.
- Two same-customer Processing reservation requests coordinated immediately before their SQL inserts: exactly one success and one ReservationAlreadyActive conflict; one active row persists.
- Both Cancelled and Confirmed prior reservations allow a later Processing reservation.
- Concurrent orders for the same reservation: exactly one success; the approved unique order/reservation constraint is also exercised directly.
- Failure after real SQL writes for each of the eight required operations restores the entire baseline, including statuses, cascades and audits.
- Concurrent initiative completions leave one Completed winner and one Cancelled loser.
- A real SQL deadlock yields one safe conflict; neither transaction delegate is replayed.
- Lost commit acknowledgement leaves one committed row and executes the delegate once.

Reproduction (PowerShell, with Docker Desktop running):

```powershell
$env:BROUJ_RUN_SQLSERVER_TESTS = '1'
dotnet test tests/Brouj.Infrastructure.IntegrationTests/Brouj.Infrastructure.IntegrationTests.csproj
dotnet test tests/Brouj.Application.UnitTests/Brouj.Application.UnitTests.csproj
dotnet test tests/Brouj.API.IntegrationTests/Brouj.API.IntegrationTests.csproj
dotnet test tests/Brouj.Domain.UnitTests/Brouj.Domain.UnitTests.csproj
```

To run only the Part 9 database tests, append `--filter 'FullyQualifiedName~Part9'` to the Infrastructure command. Final verification also passed `git diff --check` and `dotnet format whitespace ... --verify-no-changes` scoped to the 35 changed C# files, excluding the owner's pre-existing edit. The implementation inventory was compared with the working tree and matched. `dotnet list Brouj.slnx package --vulnerable --include-transitive` reported no known vulnerable packages in the configured sources; no packages were added.

## Changed files

Production:

- `src/Brouj.Application/Abstractions/Persistence/IApplicationDbContext.cs`
- `src/Brouj.Application/Abstractions/Persistence/PersistenceConflict.cs` (new)
- `src/Brouj.Application/Abstractions/Persistence/PersistenceConflictException.cs` (new)
- `src/Brouj.Application/Behaviors/FeatureFailureBehavior.cs`
- `src/Brouj.Application/Features/Projects/Commands/Create/CreateProjectCommand.cs`
- `src/Brouj.Application/Features/Units/Commands/Create/CreateUnitCommand.cs`
- `src/Brouj.Application/Features/Reservations/Commands/Create/CreateReservationCommand.cs`
- `src/Brouj.Application/Features/Orders/Commands/CreateFromReservation/CreateOrderFromReservationCommand.cs`
- `src/Brouj.Application/Features/Orders/Commands/CreateFromReservation/CreateOrderFromReservationCommandHandler.cs`
- `src/Brouj.Application/Features/Orders/Commands/CompleteInitiative/CompleteInitiativeOrderCommand.cs`
- `src/Brouj.Application/Features/Orders/Commands/CompleteInitiative/CompleteInitiativeOrderCommandHandler.cs`
- `src/Brouj.Application/Features/TimePlans/Commands/Create/CreateTimePlanCommand.cs`
- `src/Brouj.Application/Features/TimePlans/Commands/Delete/DeleteTimePlanCommand.cs`
- `src/Brouj.Application/Features/TimePlans/Commands/AddStageFromTemplate/AddStageFromTemplateCommand.cs`
- `src/Brouj.Infrastructure/DependencyInjection.cs`
- `src/Brouj.Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/Brouj.Infrastructure/Persistence/ApplicationTransactionManager.cs`
- `src/Brouj.Infrastructure/Persistence/NonReplayingExecutionStrategy.cs` (new)

Tests:

- `tests/Brouj.Application.UnitTests/Features/Part9ConflictMappingTests.cs` (new)
- `tests/Brouj.Application.UnitTests/Features/CommittedEffectTests.cs`
- `tests/Brouj.Application.UnitTests/Features/Support/FeatureTestDb.cs`
- `tests/Brouj.API.IntegrationTests/Part9RetryHttpTests.cs` (new)
- `tests/Brouj.API.IntegrationTests/ApplicationHttpTests.cs`
- `tests/Brouj.API.IntegrationTests/ApiHardeningTests.cs`
- `tests/Brouj.API.IntegrationTests/Support/ApiTestDb.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/ApplicationFeaturePipeline.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/RefreshTokenPersistenceTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9ConcurrencyTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9AtomicityTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9TransactionTests.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9FeatureHost.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9FileStorage.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9RequestRendezvous.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9SaveFailureInterceptor.cs` (new)
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/Part9CommitFailureInterceptor.cs` (new)

Existing test constructors now supply typed loggers. Application/API query doubles explicitly do not emulate SQL locks. HTTP no-write assertions compare with the startup save count because the existing host seeder saves before requests begin; production seeding was not changed.

Documentation: this file.

The pre-existing owner change in `src/Brouj.Application/Common/Models/ApiErrorResponse.cs` was preserved and is excluded from this implementation inventory.

## Definition of Done

- [x] Regular-unit double-order race prevented by a SQL unit lock and eligibility recheck; independent-context concurrency tests passed.
- [x] Duplicate active reservation race blocked by the approved filtered unique database index; both insert contenders reach SQL and one maps safely to a conflict.
- [x] Expected uniqueness and concurrency failures map to safe business conflicts without provider details.
- [x] No process-local lock is used for application correctness across instances.
- [x] All eight critical transaction boundaries are atomic under injected failure; audits and deferred file cleanup are covered.
- [x] Real SQL Server concurrency integration tests are green; complete Infrastructure suite is green.

No unresolved implementation or test blockers remain. Persistent idempotency response storage is the deferred approval item described above. No future Part has been inspected or implemented.
