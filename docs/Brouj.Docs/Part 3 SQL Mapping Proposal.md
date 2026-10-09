# Part 3 SQL mapping proposal

Status: **Approved by the owner in this conversation, including the final conflict resolutions.** Created 2026-10-09.

This document records the approved SQL column types, string lengths, decimal precision/scale, and timestamp precision for every scalar property in the current Domain model: **26 entities, 181 scalar properties**. SQL Server integration verification remains pending.

CLR types and nullability come directly from `src/Brouj.Domain/Entities`. Each row uses the existing property name as the column name. Table names, relationships, delete behaviors, uniqueness, and additional indexes remain those approved in Part 3, with the owner overrides recorded below.

## Decisions already approved by the owner

- `ActivityLog.OldData` and `ActivityLog.NewData`: nullable JSON strings stored in `nvarchar(max)`.
- Terms and conditions filtered uniqueness uses `[IsActive] = 1`.
- Standalone FK indexes rely on EF Core conventions. Entity configurations explicitly keep the approved additional, composite, and unique indexes, including both refresh-token `Token` and `CustomerId/UserId, IsActive` indexes.
- No explicit standalone `IX_CustomerRefreshTokens_CustomerId` or `IX_UserRefreshTokens_UserId` index.
- No Docker installation. SQL Server Testcontainers verification remains pending because the current environment has no compatible container runtime.
- Run available unit, model, configuration, and mapping verification without Docker. Do not substitute EF InMemory for relational tests.
- Part 3 may be implementation-complete after non-Docker verification passes, while relational verification remains explicitly pending.

EF Core may omit a standalone FK index when its columns are already covered by a key or another index. Final model tests will verify FK index coverage and absence of redundant indexes.

## Approved mapping summary

The owner approved the updated mappings and resolved the remaining conflicts in the conversation.

| Fields | Proposed SQL type |
|---|---|
| All GUID identifiers and foreign keys | `uniqueidentifier` |
| All required and optional DateTimeOffset properties | `datetimeoffset(3)` |
| All integer properties | `int` |
| All boolean properties | `bit` |
| All `Name` properties | `nvarchar(100)` |
| Customer `FName`, `LName` | `nvarchar(100)` |
| Customer/User `Email` | `varchar(254)` |
| Customer/User `Phone` | `varchar(32)` |
| Customer `IdentityNumber` | `varchar(64)` |
| Customer/User `HashPassword` | `varchar(256)` |
| Customer/User refresh-token `Token` | `varchar(450)` |
| BaseUnit/Reservation/Order `Number` | `nvarchar(64)` |
| StageTemplate/ItemTemplate `Code` | `nvarchar(64)` |
| ProjectImage/UnitImage/StageItemImage `ImagePath` | `nvarchar(2048)` |
| ProjectImage/UnitImage `AltText` | `nvarchar(100)` |
| AreaAmenity/ProjectAmenity/UnitAmenity `Value` | `nvarchar(32)` |
| BaseProject/StageItemImage `Description` | `nvarchar(max)` |
| TermsAndConditions `Title` | `nvarchar(300)` |
| TermsAndConditions `Content` | `nvarchar(max)` |
| ActivityLog `EntityType` | `nvarchar(128)` |
| ActivityLog `OldData`, `NewData` | `nvarchar(max)` (already approved) |
| All six converted enum properties | `varchar(16)` |
| BaseProject `Price`, `GaragePrice`; BaseUnit `MeterPrice`; InitiativeProject `NeighborPrice`; InitiativeUnit `NeighborMeterPrice`; Order `TotalPrice` | `decimal(19,4)` |
| BaseProject/BaseUnit `Size` | `decimal(18,4)` |
| InitiativeUnit `Percentage` | `decimal(9,6)` |
| BaseProject `Latitude`, `Longitude` | `decimal(10,7)` |

## Rationale and boundaries

- Unicode text columns preserve Arabic and other multilingual text. The owner selected non-Unicode storage for email, phone, identity number, password hash, refresh token, and enum columns. Lengths are storage capacities, not new business validation rules.
- Indexed text is bounded. The customer name composite index has up to 400 bytes of text; the area city/name composite index has up to 216 bytes of GUID and text. No approved index key exceeds even the 900-byte index key bound.
- Email length 254, phone length 32, and identity length 64 are proposed capacities; they do not add a format restriction or select an identity-number format.
- Refresh tokens use `varchar(450)`, with a maximum encoded capacity of 450 bytes. This does not prescribe token generation, hashing, or cryptography. Any later token representation must fit the approved storage capacity.
- Password hashes use `varchar(256)` without selecting a password hashing implementation.
- All enum columns use a consistent capacity of 16, covering every currently approved DB string, including `super admin`. The existing Domain extension methods remain the exact, case-sensitive conversion contract.
- Prices use 15 integral digits and four fractional digits; sizes use 14 integral digits and four fractional digits; percentages use three integral digits and six fractional digits. The percentage mapping does not define whether the application expresses percentages as ratios or percentage points.
- All DateTimeOffset columns use `datetimeoffset(3)`, preserving three fractional second digits and their offset. Audit interceptors use UTC as already required by Part 3.
- Nullability follows the Domain model exactly. In particular, the existing nullable `InitiativeProject.UpdatedAt` and `InitiativeUnit.UpdatedAt` properties are retained.
- No SQL default values, check constraints, additional unique constraints, column collations, schema changes, or migration SQL are proposed.
- `BaseProject.IsGarage = true` is an existing CLR initializer; it does not create a database default.
- Audit JSON is serialized text in the approved columns. No native SQL JSON type or new database JSON check constraint is proposed.
- Ordinary GUID `Id` primary keys can use EF Core's client-side key generation convention, without a SQL default. Shared `InitiativeProject.ProjectId` and `InitiativeUnit.UnitId` keys come from their principal entities.

## Complete scalar mapping table

This table lists every persisted scalar property. Navigation properties are represented by the approved FK relationships and are not separate columns. `NULL` means optional; `NOT NULL` means required.

| Entity | Column | Existing CLR type | Proposed SQL type | Nullability | Notes |
|---|---|---|---|---|---|
| ActivityLog | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| ActivityLog | `EntityType` | `string` | `nvarchar(128)` | NOT NULL |  |
| ActivityLog | `EntityId` | `Guid` | `uniqueidentifier` | NOT NULL | Audited entity identifier; no FK. |
| ActivityLog | `UserId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| ActivityLog | `Action` | `ActivityAction` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| ActivityLog | `NewData` | `string?` | `nvarchar(max)` | NULL | JSON text storage already approved; nullable. |
| ActivityLog | `OldData` | `string?` | `nvarchar(max)` | NULL | JSON text storage already approved; nullable. |
| ActivityLog | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| Area | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| Area | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| Area | `CityId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| Area | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| Area | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| AreaAmenity | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| AreaAmenity | `AreaId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| AreaAmenity | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| AreaAmenity | `Value` | `string` | `nvarchar(32)` | NOT NULL |  |
| AreaAmenity | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| AreaAmenity | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| BaseProject | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| BaseProject | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| BaseProject | `AreaId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| BaseProject | `Description` | `string?` | `nvarchar(max)` | NULL |  |
| BaseProject | `Latitude` | `decimal?` | `decimal(10,7)` | NULL |  |
| BaseProject | `Longitude` | `decimal?` | `decimal(10,7)` | NULL |  |
| BaseProject | `Size` | `decimal` | `decimal(18,4)` | NOT NULL |  |
| BaseProject | `ProjectType` | `ProjectType` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| BaseProject | `Price` | `decimal` | `decimal(19,4)` | NOT NULL |  |
| BaseProject | `FloorCount` | `int` | `int` | NOT NULL |  |
| BaseProject | `PlannedUnitCount` | `int` | `int` | NOT NULL |  |
| BaseProject | `GaragePrice` | `decimal?` | `decimal(19,4)` | NULL |  |
| BaseProject | `IsGarage` | `bool` | `bit` | NOT NULL |  |
| BaseProject | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| BaseProject | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| BaseUnit | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| BaseUnit | `ProjectId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| BaseUnit | `Number` | `string` | `nvarchar(64)` | NOT NULL |  |
| BaseUnit | `MeterPrice` | `decimal` | `decimal(19,4)` | NOT NULL |  |
| BaseUnit | `FloorNumber` | `int` | `int` | NOT NULL |  |
| BaseUnit | `Size` | `decimal` | `decimal(18,4)` | NOT NULL |  |
| BaseUnit | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| BaseUnit | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| City | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| City | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| City | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| City | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| Customer | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| Customer | `FName` | `string` | `nvarchar(100)` | NOT NULL |  |
| Customer | `LName` | `string` | `nvarchar(100)` | NOT NULL |  |
| Customer | `Phone` | `string` | `varchar(32)` | NOT NULL |  |
| Customer | `IdentityNumber` | `string` | `varchar(64)` | NOT NULL |  |
| Customer | `Email` | `string` | `varchar(254)` | NOT NULL |  |
| Customer | `HashPassword` | `string` | `varchar(256)` | NOT NULL |  |
| Customer | `DeletedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL |  |
| Customer | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| Customer | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| CustomerRefreshToken | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| CustomerRefreshToken | `CustomerId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| CustomerRefreshToken | `Token` | `string` | `varchar(450)` | NOT NULL |  |
| CustomerRefreshToken | `IsActive` | `bool` | `bit` | NOT NULL |  |
| CustomerRefreshToken | `ExpDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| CustomerRefreshToken | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| InitiativeProject | `ProjectId` | `Guid` | `uniqueidentifier` | NOT NULL | PK + FK to BaseProjects.Id; shared key. |
| InitiativeProject | `NeighborPrice` | `decimal` | `decimal(19,4)` | NOT NULL |  |
| InitiativeProject | `DrawDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| InitiativeProject | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| InitiativeProject | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| InitiativeUnit | `UnitId` | `Guid` | `uniqueidentifier` | NOT NULL | PK + FK to BaseUnits.Id; shared key. |
| InitiativeUnit | `NeighborMeterPrice` | `decimal` | `decimal(19,4)` | NOT NULL |  |
| InitiativeUnit | `Percentage` | `decimal` | `decimal(9,6)` | NOT NULL |  |
| InitiativeUnit | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| InitiativeUnit | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| ItemTemplate | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| ItemTemplate | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| ItemTemplate | `Code` | `string` | `nvarchar(64)` | NOT NULL |  |
| ItemTemplate | `IsActive` | `bool` | `bit` | NOT NULL |  |
| ItemTemplate | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| ItemTemplate | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| Order | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| Order | `ReservationId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| Order | `Status` | `OrderStatus` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| Order | `Number` | `string` | `nvarchar(64)` | NOT NULL |  |
| Order | `TotalPrice` | `decimal` | `decimal(19,4)` | NOT NULL |  |
| Order | `CompletedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL |  |
| Order | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| Order | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| PlanStageItem | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| PlanStageItem | `StageId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| PlanStageItem | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| PlanStageItem | `Status` | `PlanStageItemStatus` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| PlanStageItem | `SortOrder` | `int` | `int` | NOT NULL |  |
| PlanStageItem | `ConfirmedDate` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL |  |
| PlanStageItem | `DueDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| PlanStageItem | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| PlanStageItem | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| ProjectAmenity | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| ProjectAmenity | `ProjectId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| ProjectAmenity | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| ProjectAmenity | `Value` | `string` | `nvarchar(32)` | NOT NULL |  |
| ProjectAmenity | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| ProjectAmenity | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| ProjectImage | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| ProjectImage | `ProjectId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| ProjectImage | `ImagePath` | `string` | `nvarchar(2048)` | NOT NULL |  |
| ProjectImage | `AltText` | `string?` | `nvarchar(100)` | NULL |  |
| ProjectImage | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| ProjectImage | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| Reservation | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| Reservation | `UnitId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| Reservation | `CustomerId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| Reservation | `ReservationDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| Reservation | `Number` | `string` | `nvarchar(64)` | NOT NULL |  |
| Reservation | `Status` | `ReservationStatus` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| Reservation | `CancelledAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL |  |
| Reservation | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| Reservation | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| StageItemImage | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| StageItemImage | `ItemId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| StageItemImage | `ImagePath` | `string` | `nvarchar(2048)` | NOT NULL |  |
| StageItemImage | `Description` | `string?` | `nvarchar(max)` | NULL |  |
| StageItemImage | `SortOrder` | `int` | `int` | NOT NULL |  |
| StageItemImage | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| StageItemImage | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| StageTemplate | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| StageTemplate | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| StageTemplate | `Code` | `string` | `nvarchar(64)` | NOT NULL |  |
| StageTemplate | `IsActive` | `bool` | `bit` | NOT NULL |  |
| StageTemplate | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| StageTemplate | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| StageTemplateItem | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| StageTemplateItem | `StageTemplateId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| StageTemplateItem | `ItemTemplateId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| StageTemplateItem | `SortOrder` | `int` | `int` | NOT NULL |  |
| TermsAndConditions | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| TermsAndConditions | `Title` | `string` | `nvarchar(300)` | NOT NULL |  |
| TermsAndConditions | `Content` | `string` | `nvarchar(max)` | NOT NULL |  |
| TermsAndConditions | `VersionNumber` | `int` | `int` | NOT NULL |  |
| TermsAndConditions | `IsActive` | `bool` | `bit` | NOT NULL |  |
| TermsAndConditions | `EffectiveDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| TermsAndConditions | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| TermsAndConditions | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| TimePlan | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| TimePlan | `ProjectId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| TimePlan | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| TimePlan | `DeliveryDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| TimePlan | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| TimePlan | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| TimePlanStage | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| TimePlanStage | `TimePlanId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| TimePlanStage | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| TimePlanStage | `SortOrder` | `int` | `int` | NOT NULL |  |
| TimePlanStage | `DeliveryDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| TimePlanStage | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| TimePlanStage | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| UnitAmenity | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| UnitAmenity | `UnitId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| UnitAmenity | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| UnitAmenity | `Value` | `string` | `nvarchar(32)` | NOT NULL |  |
| UnitAmenity | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| UnitAmenity | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| UnitImage | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| UnitImage | `UnitId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| UnitImage | `ImagePath` | `string` | `nvarchar(2048)` | NOT NULL |  |
| UnitImage | `AltText` | `string?` | `nvarchar(100)` | NULL |  |
| UnitImage | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| UnitImage | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| User | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| User | `Name` | `string` | `nvarchar(100)` | NOT NULL |  |
| User | `Email` | `string` | `varchar(254)` | NOT NULL |  |
| User | `Phone` | `string` | `varchar(32)` | NOT NULL |  |
| User | `Role` | `UserRole` | `varchar(16)` | NOT NULL | Exact Domain enum string converter; proposed length. |
| User | `HashPassword` | `string` | `varchar(256)` | NOT NULL |  |
| User | `DeletedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL |  |
| User | `UpdatedAt` | `DateTimeOffset?` | `datetimeoffset(3)` | NULL | Optional audit timestamp. |
| User | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |
| UserRefreshToken | `Id` | `Guid` | `uniqueidentifier` | NOT NULL | PK. |
| UserRefreshToken | `UserId` | `Guid` | `uniqueidentifier` | NOT NULL | FK per Part 3. |
| UserRefreshToken | `Token` | `string` | `varchar(450)` | NOT NULL |  |
| UserRefreshToken | `IsActive` | `bool` | `bit` | NOT NULL |  |
| UserRefreshToken | `ExpDate` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL |  |
| UserRefreshToken | `CreatedAt` | `DateTimeOffset` | `datetimeoffset(3)` | NOT NULL | Required audit timestamp. |

## Enum string contract

| Property | Approved persisted strings |
|---|---|
| BaseProject.ProjectType | `regular`, `initiative` |
| Reservation.Status | `processing`, `cancelled`, `confirmed` |
| Order.Status | `pending`, `processing`, `completed`, `cancelled` |
| PlanStageItem.Status | `pending`, `processing`, `completed` |
| User.Role | `super admin`, `admin` |
| ActivityLog.Action | `created`, `updated`, `deleted` |

The active-reservation filtered unique index retains `[Status] = 'processing'`.

## Verification after approval and implementation

- Build the affected projects and run Domain/Application unit tests.
- Run non-Docker Infrastructure model/configuration/converter/audit preparation tests, including all 181 column types and nullability, all 26 configurations, keys, FK coverage, approved indexes and filters, delete behavior metadata, all enum values, and JSON storage metadata.
- Perform the dependency vulnerability scan required by the global rules.
- Add the required real SQL Server Testcontainers tests and mark their execution pending in this environment.
- Metadata and unit verification cannot establish actual SQL constraint enforcement, database cascades, transaction atomicity, rollback behavior, or generated-ID audit persistence. Those checks remain pending SQL Server verification.
- No work from any later Part.

## Sources

The proposed capacities are engineering recommendations for owner review, not values supplied by these sources.

- Current Domain entity source files and the owner-provided Part 3 contract.
- [EF Core entity properties](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties): Unicode mapping, lengths, and precision configuration.
- [EF Core foreign key index conventions](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/conventions#foreign-key-index-convention): convention-created indexes and coverage by existing keys/indexes.
- [SQL Server maximum capacity specifications](https://learn.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver17): index key capacity.

## Approval record

The owner supplied the updated mapping table and then explicitly resolved email storage as `varchar(254)`, nullable coordinate CLR types as `decimal?` with SQL `decimal(10,7)`, password hashes as `varchar(256)`, timestamps as `datetimeoffset(3)`, refresh tokens as `varchar(450)`, and the total as 181 scalar columns.

The implementation uses these approved values. No new SQL mapping decision remains outstanding.

