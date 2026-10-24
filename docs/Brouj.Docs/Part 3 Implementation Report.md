# Part 3 implementation and verification report

Status: **Implementation complete under the owner's non-Docker completion criteria. SQL Server integration verification remains pending.**

## Implemented behavior

- `ApplicationDbContext` implements `IApplicationDbContext` and exposes all 26 approved entity sets.
- All 181 scalar columns use the owner-approved SQL types, lengths, precision/scale, and nullability, including nullable `BaseProject.Latitude` and `Longitude` as `decimal(10,7)`. The mapping proposal was updated to record the owner's conflict resolutions.
- Each entity has a separate configuration applied through `ApplyConfigurationsFromAssembly`; no entity mappings are inline in `OnModelCreating`.
- All 26 primary keys, 22 foreign keys, and the exact 47-index model are verified. Standalone FK indexes rely on conventions and are omitted when already covered. All approved additional/unique indexes remain explicit.
- Delete behavior matches all 22 rows of the approved matrix. No cascade path reaches `Reservation`.
- The processing-reservation filtered unique index uses `[Status] = 'processing'`. Terms filtered uniqueness uses `[IsActive] = 1`.
- All six enum converters call the existing Domain extensions. All 17 approved members round-trip to their exact strings; unknown members, unknown stored strings, and incorrect case fail explicitly.
- `OldData` and `NewData` are optional serialized JSON strings in `nvarchar(max)`.

## Audit behavior

- Audit preparation replaces client-supplied creation/update timestamps, uses UTC with millisecond precision, clears `UpdatedAt` on insert, and preserves original `CreatedAt` on update.
- Staff logging requires a trusted authenticated `ICurrentUser` with a non-empty staff UserId, a known Admin/SuperAdmin role, and no CustomerId. Customer/anonymous contexts do not create staff logs.
- Staff logs are added to the same context and SaveChanges operation as the business changes. `AutoTransactionBehavior.Always` ensures an automatic transaction; existing SQL Server transactions use EF savepoints. Staff auditing rejects configurations that weaken the automatic transaction setting or enable MARS.
- Approved primary keys are GUIDs generated client-side by EF, or shared GUIDs inherited from principals. Temporary, empty, or unsupported keys are rejected before any SQL or audit row is added.
- Logs never recursively audit `ActivityLog` changes. Failed/cancelled save preparation discards interceptor-generated pending logs, so retries do not retain false or duplicate prepared audit entries.
- JSON snapshots include GUIDs, numbers, booleans, timestamps, approved enum strings, and changed property names. **All free-form text values are omitted**, including ordinary names/descriptions, because text can contain pasted passwords, tokens, identity numbers, or other secrets. Navigations and arbitrary objects are never serialized.
- Suppressed-save tests verify tracking and preparation only; they do not claim actual persistence, constraint enforcement, transaction atomicity, or rollback verification.

## Verification executed

Final command:

```powershell
dotnet test Brouj.slnx --no-restore --logger 'console;verbosity=minimal' --logger trx --results-directory TestResults/Part3
```

| Project | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Brouj.Domain.UnitTests | 24 | 0 | 0 |
| Brouj.Application.UnitTests | 24 | 0 | 0 |
| Brouj.Infrastructure.IntegrationTests | 257 | 0 | 18 |
| Total | **305** | **0** | **18** |

The skipped entries are SQL Server test methods; several are theories containing multiple relational cases. The existing API integration test project contains no tests. Its build/test invocation completed without discovering any tests.

Additional verification:

- The affected projects built with no compiler or analyzer warnings in the final run.
- All 181 rows in the updated mapping proposal match the approved verification fixture.
- `git diff --check` passed.
- `dotnet list Brouj.slnx package --vulnerable --include-transitive --no-restore` reported no vulnerable packages for any solution project using the current NuGet advisory sources.
- TRX result files are available under `TestResults/Part3/` (ignored build/test artifacts).
- No Docker runtime was installed. No EF InMemory or SQLite replacement was introduced.
- No migration SQL was created. Schema creation in the pending integration tests uses EF `EnsureCreatedAsync` in isolated disposable test databases.

## Definition of Done review

| Requirement | Status / evidence |
|---|---|
| Context implements Application persistence contract and exposes all entities | Verified by configuration and DI tests |
| One configuration per entity, assembly-applied mappings | 26 configurations; model tests pass |
| Approved PKs and FKs | 26 PKs / 22 FKs verified |
| Approved indexes and unique rules, no redundant FK indexes | Exact 47-index model verified |
| No unapproved columns, defaults, checks, alternate keys, lengths, precision, or collations | Owner-approved 181-column fixture and model tests pass |
| Delete matrix and no cascade reaching Reservation | All 22 relationships and cascade paths verified as metadata; SQL enforcement pending |
| InitiativeProject.ProjectId and InitiativeUnit.UnitId are PK + FK | Verified; independent key generation disabled |
| TimePlan.ProjectId, Order.ReservationId, Reservation.Number, Order.Number uniqueness | Unique mappings verified; SQL enforcement tests pending |
| Exact processing filter | Verified as `[Status] = 'processing'`; real concurrency test pending |
| Six enum converters and explicit unknown-value failures | Non-Docker conversion tests pass; real storage/materialization tests pending |
| OldData/NewData JSON mapping | Column metadata and JSON preparation verified; real storage pending |
| AuditableEntityInterceptor | Insert/update timestamp preparation tests pass; SQL persistence pending |
| ActivityLogInterceptor | Staff/customer selection, sensitive-data exclusion, no recursion, GUID safety, failure cleanup, transaction configuration guards verified; SQL atomicity/rollback pending |
| Required SQL Server Testcontainers tests green | **Pending environment requirement; not claimed green** |
| Only Part 3 implemented | Infrastructure, its tests, and Part 3 documentation only; no future Part inspected or implemented |

## Pending SQL Server verification

The current environment has no Docker Desktop or compatible running container runtime, and no real SQL Server test connection was supplied. The suite explicitly skips SQL Server tests until enabled.

Pending checks include:

- Real catalog confirmation of all column types/nullability and delete actions.
- Actual uniqueness violations, processing-reservation concurrency, cancellation followed by a new reservation, soft-delete contact reuse, and active terms version uniqueness.
- Exact enum string storage/materialization and unknown-value materialization failures.
- Restrict/cascade delete enforcement and preservation of reserved units/reservations.
- Persisted UTC timestamps, correct generated audit EntityId, JSON payloads, staff/customer distinction, and exclusion of sensitive values.
- Transaction rollback, failed-save atomicity, safe retry, and existing-transaction savepoint behavior.

With a compatible container runtime available:

```powershell
$env:BROUJ_RUN_SQLSERVER_TESTS = '1'
dotnet test tests/Brouj.Infrastructure.IntegrationTests/Brouj.Infrastructure.IntegrationTests.csproj --filter 'Category=SqlServer'
```

Alternatively, set `BROUJ_SQLSERVER_TEST_CONNECTION_STRING` to a dedicated SQL Server test environment using environment/secret storage, then run the same command. Do not commit that connection string.

The external test login must be allowed to create and drop databases. Each test creates a new `BroujPart3_<random GUID>` database; cleanup drops only that generated database and never the database named in the supplied connection string. Without an external connection, the fixture uses SQL Server Testcontainers.

Enabled tests fail normally if the server/container cannot start; they do not silently substitute a provider or report the environment as verified.

## Files added

Production files (36):

- `src/Brouj.Infrastructure/DependencyInjection.cs`
- `src/Brouj.Infrastructure/Interceptors/AuditableEntityInterceptor.cs`
- `src/Brouj.Infrastructure/Interceptors/ActivityLogInterceptor.cs`
- `src/Brouj.Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/ActivityLogConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/AreaConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/AreaAmenityConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/BaseProjectConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/BaseUnitConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/CityConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/CustomerRefreshTokenConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/InitiativeProjectConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/InitiativeUnitConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/ItemTemplateConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/PlanStageItemConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/ProjectAmenityConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/ProjectImageConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/ReservationConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/StageItemImageConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/StageTemplateConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/StageTemplateItemConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/TermsAndConditionsConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/TimePlanConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/TimePlanStageConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/UnitAmenityConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/UnitImageConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Configurations/UserRefreshTokenConfiguration.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/ActivityActionConverter.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/OrderStatusConverter.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/PlanStageItemStatusConverter.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/ProjectTypeConverter.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/ReservationStatusConverter.cs`
- `src/Brouj.Infrastructure/Persistence/Converters/UserRoleConverter.cs`

Test and verification files (18):

- `tests/Brouj.Infrastructure.IntegrationTests/DependencyInjectionTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/ApprovedColumns.tsv`
- `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/ApprovedIndexes.tsv`
- `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/TestActor.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Fixtures/TestContext.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Interceptors/AuditPreparationTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Model/EntityConfigurationTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Model/EnumConverterTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Model/IndexTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/Model/RelationshipTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/SqlServerFactAttribute.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/SqlServerFixture.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/TestGraph.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/ConstraintTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/DeleteBehaviorTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/AuditPersistenceTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/EnumPersistenceTests.cs`
- `tests/Brouj.Infrastructure.IntegrationTests/SqlServer/SchemaPersistenceTests.cs`

Documentation:

- `docs/Brouj.Docs/Part 3 SQL Mapping Proposal.md`: approved mapping table and owner resolution record.
- `docs/Brouj.Docs/Part 3 Implementation Report.md`: this report.

## Existing files changed

- `src/Brouj.Infrastructure/Brouj.Infrastructure.csproj`: SQL Server EF Core provider 10.0.11, aligned with the existing Application EF Core version.
- `tests/Brouj.Infrastructure.IntegrationTests/Brouj.Infrastructure.IntegrationTests.csproj`: Testcontainers.MsSql 4.15.0 and copied approved verification fixtures.

The owner's pre-existing changes in `README.md` and `src/Brouj.Domain/Entities/BaseProject.cs` were preserved. The latter already contained the owner-requested Latitude/Longitude replacement and was not modified by this implementation.

## Blockers and approval items

- Implementation blockers: **none**.
- Verification blocker: **real SQL Server integration checks are pending the environment described above**.
- Outstanding SQL mapping decisions: **none**.
- Further owner approval is needed before starting any later Part, per Global Roles. No later Part work has begun.
