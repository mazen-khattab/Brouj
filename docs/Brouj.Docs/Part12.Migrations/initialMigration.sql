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
CREATE TABLE [Cities] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_Cities] PRIMARY KEY ([Id])
);

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

CREATE TABLE [ItemTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(64) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_ItemTemplates] PRIMARY KEY ([Id])
);

CREATE TABLE [StageTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(64) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_StageTemplates] PRIMARY KEY ([Id])
);

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

CREATE TABLE [Areas] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [CityId] uniqueidentifier NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_Areas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Areas_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION
);

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

CREATE TABLE [StageTemplateItems] (
    [Id] uniqueidentifier NOT NULL,
    [StageTemplateId] uniqueidentifier NOT NULL,
    [ItemTemplateId] uniqueidentifier NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_StageTemplateItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StageTemplateItems_ItemTemplates_ItemTemplateId] FOREIGN KEY ([ItemTemplateId]) REFERENCES [ItemTemplates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StageTemplateItems_StageTemplates_StageTemplateId] FOREIGN KEY ([StageTemplateId]) REFERENCES [StageTemplates] ([Id]) ON DELETE CASCADE
);

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

CREATE TABLE [InitiativeProjects] (
    [ProjectId] uniqueidentifier NOT NULL,
    [NeighborPrice] decimal(19,4) NOT NULL,
    [DrawDate] datetimeoffset(3) NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_InitiativeProjects] PRIMARY KEY ([ProjectId]),
    CONSTRAINT [FK_InitiativeProjects_BaseProjects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [BaseProjects] ([Id]) ON DELETE CASCADE
);

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

CREATE TABLE [InitiativeUnits] (
    [UnitId] uniqueidentifier NOT NULL,
    [NeighborMeterPrice] decimal(19,4) NOT NULL,
    [Percentage] decimal(9,6) NOT NULL,
    [CreatedAt] datetimeoffset(3) NOT NULL,
    [UpdatedAt] datetimeoffset(3) NULL,
    CONSTRAINT [PK_InitiativeUnits] PRIMARY KEY ([UnitId]),
    CONSTRAINT [FK_InitiativeUnits_BaseUnits_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [BaseUnits] ([Id]) ON DELETE CASCADE
);

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

CREATE INDEX [IX_ActivityLogs_Action_CreatedAt] ON [ActivityLogs] ([Action], [CreatedAt]);

CREATE INDEX [IX_ActivityLogs_CreatedAt] ON [ActivityLogs] ([CreatedAt]);

CREATE INDEX [IX_ActivityLogs_EntityType_EntityId_CreatedAt] ON [ActivityLogs] ([EntityType], [EntityId], [CreatedAt]);

CREATE INDEX [IX_ActivityLogs_UserId_CreatedAt] ON [ActivityLogs] ([UserId], [CreatedAt]);

CREATE INDEX [IX_AreaAmenities_AreaId] ON [AreaAmenities] ([AreaId]);

CREATE UNIQUE INDEX [UQ_Areas_CityId_Name] ON [Areas] ([CityId], [Name]);

CREATE INDEX [IX_BaseProjects_AreaId] ON [BaseProjects] ([AreaId]);

CREATE INDEX [IX_BaseProjects_ProjectType] ON [BaseProjects] ([ProjectType]);

CREATE UNIQUE INDEX [UQ_BaseUnits_ProjectId_Number] ON [BaseUnits] ([ProjectId], [Number]);

CREATE UNIQUE INDEX [UQ_Cities_Name] ON [Cities] ([Name]);

CREATE UNIQUE INDEX [IX_CustomerRefreshTokens_Active] ON [CustomerRefreshTokens] ([CustomerId]) WHERE [IsActive] = 1;

CREATE UNIQUE INDEX [IX_CustomerRefreshTokens_Token] ON [CustomerRefreshTokens] ([Token]);

CREATE INDEX [IX_Customers_FName_LName] ON [Customers] ([FName], [LName]);

CREATE UNIQUE INDEX [UQ_Customers_Email] ON [Customers] ([Email]) WHERE [DeletedAt] IS NULL;

CREATE UNIQUE INDEX [UQ_Customers_IdentityNumber] ON [Customers] ([IdentityNumber]) WHERE [DeletedAt] IS NULL;

CREATE UNIQUE INDEX [UQ_Customers_Phone] ON [Customers] ([Phone]) WHERE [DeletedAt] IS NULL;

CREATE INDEX [IX_ItemTemplates_IsActive] ON [ItemTemplates] ([IsActive]);

CREATE UNIQUE INDEX [UQ_ItemTemplates_Code] ON [ItemTemplates] ([Code]);

CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);

CREATE UNIQUE INDEX [UQ_Orders_Number] ON [Orders] ([Number]);

CREATE UNIQUE INDEX [UQ_Orders_ReservationId] ON [Orders] ([ReservationId]);

CREATE INDEX [IX_PlanStageItems_StageId_Status] ON [PlanStageItems] ([StageId], [Status]);

CREATE UNIQUE INDEX [UQ_PlanStageItems_StageId_SortOrder] ON [PlanStageItems] ([StageId], [SortOrder]);

CREATE INDEX [IX_ProjectAmenities_ProjectId] ON [ProjectAmenities] ([ProjectId]);

CREATE INDEX [IX_ProjectImages_ProjectId] ON [ProjectImages] ([ProjectId]);

CREATE INDEX [IX_Reservations_CustomerId_UnitId_Status] ON [Reservations] ([CustomerId], [UnitId], [Status]);

CREATE INDEX [IX_Reservations_Status] ON [Reservations] ([Status]);

CREATE INDEX [IX_Reservations_UnitId_Status] ON [Reservations] ([UnitId], [Status]);

CREATE UNIQUE INDEX [UQ_Reservations_CustomerId_UnitId_Processing] ON [Reservations] ([CustomerId], [UnitId]) WHERE [Status] = 'processing';

CREATE UNIQUE INDEX [UQ_Reservations_Number] ON [Reservations] ([Number]);

CREATE UNIQUE INDEX [UQ_StageItemImages_ItemId_SortOrder] ON [StageItemImages] ([ItemId], [SortOrder]);

CREATE INDEX [IX_StageTemplateItems_ItemTemplateId] ON [StageTemplateItems] ([ItemTemplateId]);

CREATE UNIQUE INDEX [UQ_StageTemplateItems_StageTemplateId_ItemTemplateId] ON [StageTemplateItems] ([StageTemplateId], [ItemTemplateId]);

CREATE UNIQUE INDEX [UQ_StageTemplateItems_StageTemplateId_SortOrder] ON [StageTemplateItems] ([StageTemplateId], [SortOrder]);

CREATE INDEX [IX_StageTemplates_IsActive] ON [StageTemplates] ([IsActive]);

CREATE UNIQUE INDEX [UQ_StageTemplates_Code] ON [StageTemplates] ([Code]);

CREATE INDEX [IX_TermsAndConditions_IsActive_EffectiveDate] ON [TermsAndConditions] ([IsActive], [EffectiveDate]);

CREATE UNIQUE INDEX [UQ_TermsAndConditions_VersionNumber] ON [TermsAndConditions] ([VersionNumber]);

CREATE UNIQUE INDEX [UQ_TimePlans_ProjectId] ON [TimePlans] ([ProjectId]);

CREATE UNIQUE INDEX [UQ_TimePlanStages_TimePlanId_SortOrder] ON [TimePlanStages] ([TimePlanId], [SortOrder]);

CREATE INDEX [IX_UnitAmenities_UnitId] ON [UnitAmenities] ([UnitId]);

CREATE INDEX [IX_UnitImages_UnitId] ON [UnitImages] ([UnitId]);

CREATE UNIQUE INDEX [IX_UserRefreshTokens_Token] ON [UserRefreshTokens] ([Token]);

CREATE UNIQUE INDEX [UX_UserRefreshTokens_Active] ON [UserRefreshTokens] ([UserId]) WHERE [IsActive] = 1;

CREATE INDEX [IX_Users_Name] ON [Users] ([Name]);

CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]) WHERE [DeletedAt] IS NULL;

CREATE UNIQUE INDEX [UQ_Users_Phone] ON [Users] ([Phone]) WHERE [DeletedAt] IS NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261026163251_initialMigration', N'10.0.14');

COMMIT;
GO

