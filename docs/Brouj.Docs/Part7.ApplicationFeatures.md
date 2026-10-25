# Part 7 application features

Implemented on 2026-10-26 against the owner's updated global rules and approved business decisions. Source changes are complete; SQL Server verification and deployment remain blocked under the owner's instruction to continue with available checks.

## Architecture and file layout

- **69 commands and 32 queries**, each in its own operation folder containing exactly three files: request, validator and handler.
- Protected requests implement `IRequireAccess` with an explicit, read-only access requirement. `AuthorizationBehavior` checks all 84 protected operations before validation and transactions; 17 public operations skip account permission lookups.
- Feature DTOs and DTO validators are inside that feature's `DTOs` folder. Feature errors are inside its `Errors` folder.
- One type per new C# file, including test helpers. No combined handler files or global feature DTO collections.
- `Common/DTOs` contains only models shared by multiple features: login, password change, session metadata, amenities, upload input and catalogue image metadata. Their validators accompany them.
- Shared authorization, session response, file cleanup and upload services are under `Common/Services`; paging, pricing and validation utilities are under `Common/Utils`.
- Template snapshot construction and time-plan ordering are specific to TimePlans and live under `Features/TimePlans/Services`.
- File-scoped namespaces, ordinary constructors, required braces, whitespace around validation, and single-line signatures when they fit 120 characters. Business-rule comments explain authorization, snapshots, rounding, atomic replacements, temporary ordering and file cleanup.

See [the complete file inventory](Part7.ChangeInventory.md) and [the interface-based authorization refactor](Part7.FeatureAccessRefactor.md). A representative operation is [CreateProject](../../src/Brouj.Application/Features/Projects/Commands/Create/CreateProjectCommandHandler.cs).

No projects, package references or API endpoints were added. Domain schema and relationships are unchanged except the explicitly approved terms-version index filter removal.

## Permissions

| Caller | Allowed operations |
|---|---|
| Anonymous | Catalogue reads and current terms; customer registration; customer/staff login; refresh and logout through possession of the appropriate refresh cookie |
| Active customer | Own profile, password, reservations and orders; project time-plan read |
| Active Admin or SuperAdmin | Catalogue, time-plan, template, terms, order and customer administration; activity-log reads; own staff profile and password |
| Active SuperAdmin | Staff creation, updates, role changes, deletion, restoration, list and details |

`AuthorizationBehavior` is the single caller of `FeatureAccess` for protected dispatches. Each access requirement is fixed in the request class and cannot be bound from JSON. Handlers use `ICurrentUser` directly for ownership and self-management rules; they no longer call the permission service. Database state is authoritative for active accounts and staff roles. Stale role claims and deleted accounts are rejected. Self-deletion, self-demotion and removal/demotion of the last active SuperAdmin are guarded. Restoring legacy accounts checks active contact collisions.

Customer-owned queries apply ownership before projection. Customer IDs never come from writable request inputs. Time plans are readable by any active customer or staff member without requiring a reservation.

The permission check runs once per protected MediatR dispatch, including calls outside controllers. Public catalogue requests skip this check even when an actor is present. These are request-time application guards. Multi-request race guarantees beyond existing database constraints and authentication locking are not claimed; the Part 7 plan explicitly defers further concurrency hardening.

## Authentication and input decisions

- New passwords require at least eight characters only; no character categories, maximum length or password trimming. Password changes verify the old password and preserve the existing session.
- Egyptian mobile input uses local eleven-digit form beginning `010`, `011`, `012` or `015`. National IDs require fourteen ASCII digits; no additional birth-date/checksum policy was invented.
- Emails are validated, trimmed and lowercased before storage and lookup.
- Registration saves the account and immediately asks the existing refresh service to establish the session. The refresh service uses its own isolated context, so the account must commit first. If session issuance subsequently fails, the account remains and the customer can log in; this is not a distributed atomic account/session transaction.
- Missing, deleted and incorrect-password login attempts share the same credential error. Missing/deleted accounts still perform the trusted password hasher's verification work.
- Credentials travel only through `IAuthenticationSession`, implemented by the existing secure cookie writer in Infrastructure. Response DTOs contain actor ID and session expiry only.
- Application has no HTTP dependencies. Cookie reading/writing stays in Infrastructure; existing CSRF and cookie policies continue to apply when the host dispatches these operations.

Pagination defaults to page 1 / size 20 and caps size at 100. Collection inputs are bounded: amenities, template mappings, draft stages and draft items at 100; reorder payloads at 1,000. Upload input caps declared length at 10 MiB while the existing storage service enforces its configured limit and validates actual content. Text limits follow approved columns or bounded payload rules.

Account duplicate checks conservatively reject reuse of account details, including deleted rows. Restoration also handles legacy data that reused values through filtered database indexes.

## Catalogue and commerce

- Every unit has a garage with price zero. Those values are server controlled and included in displayed and payable totals.
- Unit total = size × meter price, rounded **up** to cents: `10.121 → 10.13`. Computed totals must fit the approved order amount column.
- Initiative percentage = `(rounded Unit.TotalPrice / Project.Price) × 100`, stored numerically without a percent sign. Values must stay from 0 to 100; initiative project price must be positive. Percentages use the approved six-decimal storage scale.
- Initiative unit edits and project-price changes recalculate percentages. Project updates validate all affected unit totals before changing tracked values.
- Neighbor project/unit prices are display values only. They do not affect payable prices.
- Project type can change only before any unit exists.
- City deletion conflicts when areas exist; area deletion conflicts when projects exist. Any reservation, including a cancelled one, protects its unit and project from deletion. Allowed trees use the existing approved database cascades.
- A reservation starts Processing with a generated number/date and current customer ID. A duplicate Processing reservation for the same customer/unit is rejected. A cancelled reservation permits a later reservation when the unit is otherwise available.
- Only a Processing reservation without an order can be cancelled. Order creation confirms the reservation.
- Staff creates one order per reservation. Regular orders start Processing; initiative orders start Pending. Order total is a calculated snapshot.
- **Only Pending orders leave a unit available. Every other status, including Cancelled, makes it unavailable**, as explicitly requested.
- Completing a pending initiative order records the winner and completion time and cancels every other order for that unit. An existing completed winner prevents another completion. No payment, lottery or general order-status module was added.

Large list queries project small DTOs with deterministic ordering. Reads use `AsNoTracking`. Project and unit details load independent amenities/images separately to avoid multiplying rows. Activity logs expose metadata only, excluding raw old/new audit JSON.

## Time plans and templates

The client keeps the unsaved draft. Each stage-template selection supplies the template identifier and manual stage delivery/item due dates. The same template may be selected repeatedly; each selection becomes an independent stage snapshot.

`CreateTimePlanCommand` accepts the complete set of draft stages and saves the graph once, after every source and date has been checked. `AddStageFromTemplateCommand` supports appending a complete dated snapshot to an existing plan. Names and default Pending item states are copied; template dates are never inherited, and snapshots have no template FK or later synchronization.

The approved schema has a plan delivery date, stage delivery date, item due date and optional item confirmed date. No extra date columns were added. Missing required dates produce **`Pls insert the dates first`**. A Completed item requires a manually supplied confirmed date; otherwise the confirmed date is optional. An admin can supply confirmed dates for any defined state. There are no comparisons between item dates and stage/plan delivery dates, and no automatic completion date assignment.

Every defined item status may transition to every other defined status, including itself. Inactive stage templates or inactive linked item templates cannot be copied. Item template membership must exactly match the selected stage template, with dates for every item.

New stages, items and images append to their siblings. Full reorder commands validate parent membership, unique IDs and contiguous one-based positions. Deletion compacts remaining positions. Multi-save ordering and template-mapping replacements use the existing transaction boundary. Reordering temporarily persists negative positions before final positive positions to avoid unique-index collisions during swaps.

## Terms and effects

Terms content is immutable history. Activation rejects future effective dates and deactivates other versions in the same save; there is no scheduler. Version numbers are unique across active and inactive rows.

The EF mapping and approved-index fixture remove only the owner-approved active filter. [The deployment script](../../src/Brouj.Infrastructure/Persistence/Scripts/Part7TermsVersionUniqueness.sql) checks legacy duplicates and multiple active versions, locks the table while replacing the index, and aborts rather than deleting or renumbering data. **It has not been executed against a database.**

Hosts must dispatch requests through MediatR to apply validation, transaction and committed-effect behaviors. Cache invalidation notifications and pending file removals run after a successful commit. A project deletion emits both catalogue and time-plan invalidation intents. Failed writes discard deferred removals; failed upload persistence attempts to remove the newly stored file with a non-cancelled token.

No feature read cache is populated by this Part. Notifications express invalidation intent; they are not a durable distributed cache-invalidation system. Post-commit cleanup/publish failures are logged without paths or secrets and do not turn a committed write into a reported failure. Cleanup is best effort; crash recovery and a durable outbox were not added.

Provider persistence errors are sanitized before reaching host logs, without retaining an inner provider exception that could expose identity data.

## Verification

| Check | Result |
|---|---|
| Solution build after formatting | Passed, zero warnings and zero errors |
| Domain tests | 24 passed |
| Application tests | 518 passed |
| Infrastructure tests available without SQL Server | 375 passed |
| API foundation regression tests | 6 passed |
| Total executed | **923 passed, 0 failed** |
| SQL Server tests | **31 blocked/skipped**, including conditional theory methods |
| Dependency vulnerability scan, including transitive packages | No known vulnerable packages reported by configured NuGet sources |
| Whitespace and diff checks | Passed for authored code / tracked changes |

Commands used:

```powershell
dotnet build Brouj.slnx --no-restore -clp:ErrorsOnly
dotnet test Brouj.slnx --no-restore --logger 'trx;LogFileName=access-refactor.trx' -clp:ErrorsOnly
dotnet list Brouj.slnx package --vulnerable --include-transitive
dotnet format whitespace src/Brouj.Application/Features --folder
git diff --check
```

The other new shared-code/test folders were formatted in the same folder mode. SDK formatting/build-host named pipes required running some checks outside the sandbox; those checks subsequently succeeded. TRX files are in each test project's ignored `TestResults` directory.

Architecture tests compare all 101 requests with the provided Part 7 plan, check every three-file operation folder, enforce one type per feature/shared support file, validate DTO/error locations and prove that common DTOs are referenced by multiple features. Anonymous-access tests exercise all 84 protected requests through the registered MediatR pipeline. Additional tests preserve every access-level declaration, cover the actor/access matrix, check one account lookup per protected dispatch and no lookup for public catalogue requests, and reject JSON permission overrides. Behavioral tests cover authentication/session boundaries, ownership, role guards, CRUD restrictions, rounding, availability, initiative winners, percentages, draft dates, snapshots, all nine item-status combinations, ordering, history, log filters, file failure cleanup and committed effects.

Application query doubles exercise handler decisions only. They do not emulate SQL Server, uniqueness, cascades or transaction rollback, and are not presented as relational proof.

Real-SQL tests are prepared for project-child rollback with audit rows, unique-position swaps and rollback of temporary ordering, DTO query translation/no tracking, project-tree deletion, and terms-number uniqueness for active and inactive versions. They remain unexecuted under the owner's earlier Option 3 decision. No SQL target, container provisioning or deployment was assumed.

## Definition of Done and remaining boundary

All approved commands, queries and validators exist. The business rules have application tests, and the available regression suite passes. No API endpoints or later concurrency implementation were added. The owner's global-rule and cache-service edits remain untouched.

Remaining blockers are **real SQL Server verification** and **applying the approved terms index change to a selected deployment database**. Existing registration/session isolation and best-effort cleanup limitations are described above. No additional business decision or source-change approval is required for the implemented Part 7 scope.
