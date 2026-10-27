IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Cities] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_Cities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] uniqueidentifier NOT NULL,
        [FName] nvarchar(100) NOT NULL,
        [LName] nvarchar(100) NOT NULL,
        [Phone] varchar(32) NOT NULL,
        [IdentityNumber] varchar(64) NOT NULL,
        [Email] varchar(254) NOT NULL,
        [HashPassword] varchar(256) NOT NULL,
        [DeletedAt] datetimeoffset(3) NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [ItemTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_ItemTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [StageTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_StageTemplates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [TermsAndConditions] (
        [Id] uniqueidentifier NOT NULL,
        [Title] nvarchar(300) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [VersionNumber] int NOT NULL,
        [IsActive] bit NOT NULL,
        [EffectiveDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_TermsAndConditions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Email] varchar(254) NOT NULL,
        [Phone] varchar(32) NOT NULL,
        [Role] varchar(16) NOT NULL,
        [HashPassword] varchar(256) NOT NULL,
        [DeletedAt] datetimeoffset(3) NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Areas] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CityId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_Areas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Areas_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [CustomerRefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Token] varchar(450) NOT NULL,
        [IsActive] bit NOT NULL,
        [ExpDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        CONSTRAINT [PK_CustomerRefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerRefreshTokens_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [StageTemplateItems] (
        [Id] uniqueidentifier NOT NULL,
        [StageTemplateId] uniqueidentifier NOT NULL,
        [ItemTemplateId] uniqueidentifier NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_StageTemplateItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StageTemplateItems_ItemTemplates_ItemTemplateId] FOREIGN KEY ([ItemTemplateId]) REFERENCES [ItemTemplates] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StageTemplateItems_StageTemplates_StageTemplateId] FOREIGN KEY ([StageTemplateId]) REFERENCES [StageTemplates] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [ActivityLogs] (
        [Id] uniqueidentifier NOT NULL,
        [EntityType] nvarchar(128) NOT NULL,
        [EntityId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Action] varchar(16) NOT NULL,
        [NewData] nvarchar(max) NULL,
        [OldData] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ActivityLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [UserRefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Token] varchar(450) NOT NULL,
        [IsActive] bit NOT NULL,
        [ExpDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        CONSTRAINT [PK_UserRefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [AreaAmenities] (
        [Id] uniqueidentifier NOT NULL,
        [AreaId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Value] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_AreaAmenities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AreaAmenities_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [BaseProjects] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [AreaId] uniqueidentifier NOT NULL,
        [Description] nvarchar(max) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [Size] decimal(18,4) NOT NULL,
        [ProjectType] varchar(16) NOT NULL,
        [Price] decimal(19,4) NOT NULL,
        [FloorCount] int NOT NULL,
        [PlannedUnitCount] int NOT NULL,
        [GaragePrice] decimal(19,4) NULL,
        [IsGarage] bit NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_BaseProjects] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BaseProjects_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [BaseUnits] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Number] nvarchar(64) NOT NULL,
        [MeterPrice] decimal(19,4) NOT NULL,
        [FloorNumber] int NOT NULL,
        [Size] decimal(18,4) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_BaseUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BaseUnits_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [InitiativeProjects] (
        [ProjectId] uniqueidentifier NOT NULL,
        [NeighborPrice] decimal(19,4) NOT NULL,
        [DrawDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_InitiativeProjects] PRIMARY KEY ([ProjectId]),
        CONSTRAINT [FK_InitiativeProjects_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [ProjectAmenities] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Value] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_ProjectAmenities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectAmenities_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [ProjectImages] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [ImagePath] nvarchar(2048) NOT NULL,
        [AltText] nvarchar(100) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_ProjectImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectImages_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [TimePlans] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DeliveryDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_TimePlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimePlans_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [InitiativeUnits] (
        [UnitId] uniqueidentifier NOT NULL,
        [NeighborMeterPrice] decimal(19,4) NOT NULL,
        [Percentage] decimal(9,6) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_InitiativeUnits] PRIMARY KEY ([UnitId]),
        CONSTRAINT [FK_InitiativeUnits_BaseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [BaseUnits] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Reservations] (
        [Id] uniqueidentifier NOT NULL,
        [UnitId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [ReservationDate] datetimeoffset(3) NOT NULL,
        [Number] nvarchar(64) NOT NULL,
        [Status] varchar(16) NOT NULL,
        [CancelledAt] datetimeoffset(3) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reservations_BaseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [BaseUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Reservations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [UnitAmenities] (
        [Id] uniqueidentifier NOT NULL,
        [UnitId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Value] nvarchar(32) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_UnitAmenities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitAmenities_BaseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [BaseUnits] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [UnitImages] (
        [Id] uniqueidentifier NOT NULL,
        [UnitId] uniqueidentifier NOT NULL,
        [ImagePath] nvarchar(2048) NOT NULL,
        [AltText] nvarchar(100) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_UnitImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitImages_BaseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [BaseUnits] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [TimePlanStages] (
        [Id] uniqueidentifier NOT NULL,
        [TimePlanId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [SortOrder] int NOT NULL,
        [DeliveryDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_TimePlanStages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimePlanStages_TimePlans_TimePlanId] FOREIGN KEY ([TimePlanId]) REFERENCES [TimePlans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [ReservationId] uniqueidentifier NOT NULL,
        [Status] varchar(16) NOT NULL,
        [Number] nvarchar(64) NOT NULL,
        [TotalPrice] decimal(19,4) NOT NULL,
        [CompletedAt] datetimeoffset(3) NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [PlanStageItems] (
        [Id] uniqueidentifier NOT NULL,
        [StageId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Status] varchar(16) NOT NULL,
        [SortOrder] int NOT NULL,
        [ConfirmedDate] datetimeoffset(3) NULL,
        [DueDate] datetimeoffset(3) NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_PlanStageItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PlanStageItems_TimePlanStages_StageId] FOREIGN KEY ([StageId]) REFERENCES [TimePlanStages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE TABLE [StageItemImages] (
        [Id] uniqueidentifier NOT NULL,
        [ItemId] uniqueidentifier NOT NULL,
        [ImagePath] nvarchar(2048) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetimeoffset(3) NOT NULL,
        [UpdatedAt] datetimeoffset(3) NULL,
        CONSTRAINT [PK_StageItemImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StageItemImages_PlanStageItems_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [PlanStageItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_Action_CreatedAt] ON [ActivityLogs] ([Action], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_CreatedAt] ON [ActivityLogs] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_EntityType_EntityId_CreatedAt] ON [ActivityLogs] ([EntityType], [EntityId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ActivityLogs_UserId_CreatedAt] ON [ActivityLogs] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_AreaAmenities_AreaId] ON [AreaAmenities] ([AreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Areas_CityId_Name] ON [Areas] ([CityId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_BaseProjects_AreaId] ON [BaseProjects] ([AreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_BaseProjects_ProjectType] ON [BaseProjects] ([ProjectType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_BaseUnits_ProjectId_Number] ON [BaseUnits] ([ProjectId], [Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Cities_Name] ON [Cities] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CustomerRefreshTokens_Active] ON [CustomerRefreshTokens] ([CustomerId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerRefreshTokens_Token] ON [CustomerRefreshTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Customers_FName_LName] ON [Customers] ([FName], [LName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Customers_Email] ON [Customers] ([Email]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Customers_IdentityNumber] ON [Customers] ([IdentityNumber]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Customers_Phone] ON [Customers] ([Phone]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ItemTemplates_IsActive] ON [ItemTemplates] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_ItemTemplates_Code] ON [ItemTemplates] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Orders_Number] ON [Orders] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Orders_ReservationId] ON [Orders] ([ReservationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_PlanStageItems_StageId_Status] ON [PlanStageItems] ([StageId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_PlanStageItems_StageId_SortOrder] ON [PlanStageItems] ([StageId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectAmenities_ProjectId] ON [ProjectAmenities] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_ProjectImages_ProjectId] ON [ProjectImages] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Reservations_CustomerId_UnitId_Status] ON [Reservations] ([CustomerId], [UnitId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Reservations_Status] ON [Reservations] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Reservations_UnitId_Status] ON [Reservations] ([UnitId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Reservations_CustomerId_UnitId_Processing] ON [Reservations] ([CustomerId], [UnitId]) WHERE [Status] = ''processing''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Reservations_Number] ON [Reservations] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_StageItemImages_ItemId_SortOrder] ON [StageItemImages] ([ItemId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_StageTemplateItems_ItemTemplateId] ON [StageTemplateItems] ([ItemTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_StageTemplateItems_StageTemplateId_ItemTemplateId] ON [StageTemplateItems] ([StageTemplateId], [ItemTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_StageTemplateItems_StageTemplateId_SortOrder] ON [StageTemplateItems] ([StageTemplateId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_StageTemplates_IsActive] ON [StageTemplates] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_StageTemplates_Code] ON [StageTemplates] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_TermsAndConditions_IsActive_EffectiveDate] ON [TermsAndConditions] ([IsActive], [EffectiveDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_TermsAndConditions_VersionNumber] ON [TermsAndConditions] ([VersionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_TimePlans_ProjectId] ON [TimePlans] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_TimePlanStages_TimePlanId_SortOrder] ON [TimePlanStages] ([TimePlanId], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_UnitAmenities_UnitId] ON [UnitAmenities] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_UnitImages_UnitId] ON [UnitImages] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserRefreshTokens_Token] ON [UserRefreshTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_UserRefreshTokens_Active] ON [UserRefreshTokens] ([UserId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    CREATE INDEX [IX_Users_Name] ON [Users] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Users_Phone] ON [Users] ([Phone]) WHERE [DeletedAt] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026163251_initialMigration'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026163251_initialMigration', N'10.0.14');
END;

COMMIT;
GO

