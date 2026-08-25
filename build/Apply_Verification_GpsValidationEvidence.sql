IF OBJECT_ID(N'[Verification].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'Verification') IS NULL EXEC(N'CREATE SCHEMA [Verification];');
    CREATE TABLE [Verification].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    IF SCHEMA_ID(N'Verification') IS NULL EXEC(N'CREATE SCHEMA [Verification];');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationCycle] (
        [Id] int NOT NULL IDENTITY,
        [CycleName] nvarchar(120) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [IsActive] bit NOT NULL,
        [ClosedOnUtc] datetime2 NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerificationCycle] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    CREATE TABLE [Verification].[PhysicalVerification] (
        [Id] int NOT NULL IDENTITY,
        [PhysicalVerificationCycleId] int NOT NULL,
        [AssetId] int NOT NULL,
        [ClientCaptureId] uniqueidentifier NULL,
        [ScannedQrValue] nvarchar(200) NULL,
        [HasQrMismatch] bit NOT NULL,
        [WorkingCondition] nvarchar(20) NOT NULL,
        [SerialVerified] bit NOT NULL,
        [GpsLatitude] decimal(9,6) NULL,
        [GpsLongitude] decimal(9,6) NULL,
        [PhotoPath] nvarchar(400) NULL,
        [LocationId] int NULL,
        [HolderEmployeeId] int NULL,
        [StatusUpdatedToId] int NULL,
        [VerifiedByUserId] int NOT NULL,
        [VerifiedOnUtc] datetime2 NOT NULL,
        [Remarks] nvarchar(500) NULL,
        [CreatedOnUtc] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedOnUtc] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerification] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PhysicalVerification_Condition] CHECK (([WorkingCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing'))),
        CONSTRAINT [FK_PhysicalVerification_PhysicalVerificationCycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    CREATE INDEX [IX_PhysicalVerification_Exceptions] ON [Verification].[PhysicalVerification] ([LocationId], [WorkingCondition]);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PhysicalVerification_ClientCapture] ON [Verification].[PhysicalVerification] ([ClientCaptureId]) WHERE [ClientCaptureId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [UX_PhysicalVerification_OnePerAssetPerCycle] ON [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId], [AssetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    CREATE UNIQUE INDEX [UX_PhysicalVerificationCycle_Name] ON [Verification].[PhysicalVerificationCycle] ([CycleName]);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PhysicalVerificationCycle_OneActive] ON [Verification].[PhysicalVerificationCycle] ([IsActive]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812063346_20260812_Verification_Initial'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812063346_20260812_Verification_Initial', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    DROP INDEX [UX_PhysicalVerification_OnePerAssetPerCycle] ON [Verification].[PhysicalVerification];
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [CountedQuantity] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [ExpectedQuantitySnapshot] decimal(18,3) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [IsBulkCount] bit NOT NULL DEFAULT (0);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PhysicalVerification_OneBulkCountPerPlacePerCycle] ON [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId], [AssetId], [LocationId]) WHERE [IsBulkCount] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PhysicalVerification_OnePerUnitAssetPerCycle] ON [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId], [AssetId]) WHERE [IsBulkCount] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_BulkHasCount] CHECK (([IsBulkCount] = 0 OR [CountedQuantity] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_CountNonNegative] CHECK (([CountedQuantity] IS NULL OR [CountedQuantity] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812091343_20260812_Verification_Revision3'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812091343_20260812_Verification_Revision3', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812092119_20260812_Verification_NamedDefaultConstraints'
)
BEGIN

                    DECLARE @n sysname, @q nvarchar(400);
                    SELECT @n = dc.name FROM sys.default_constraints dc
                           JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                    WHERE  dc.parent_object_id = OBJECT_ID(N'[Verification].[PhysicalVerification]') AND c.name = N'IsBulkCount';
                    IF @n IS NULL
                        ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [DF_PhysicalVerification_IsBulkCount] DEFAULT 0 FOR [IsBulkCount];
                    ELSE IF @n <> N'DF_PhysicalVerification_IsBulkCount'
                    BEGIN
                        SET @q = N'[Verification].[' + @n + N']';
                        EXEC sp_rename @objname = @q, @newname = N'DF_PhysicalVerification_IsBulkCount', @objtype = N'OBJECT';
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260812092119_20260812_Verification_NamedDefaultConstraints'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260812092119_20260812_Verification_NamedDefaultConstraints', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    DROP INDEX [UX_PhysicalVerificationCycle_OneActive] ON [Verification].[PhysicalVerificationCycle];
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerificationCycle] ADD [BranchId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerificationCycle] ADD [TotalAssetCount] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    UPDATE [Verification].[PhysicalVerificationCycle] SET [BranchId] = 0, [TotalAssetCount] = 0;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Verification].[PhysicalVerificationCycle]') AND [c].[name] = N'BranchId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Verification].[PhysicalVerificationCycle] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Verification].[PhysicalVerificationCycle] ALTER COLUMN [BranchId] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Verification].[PhysicalVerificationCycle]') AND [c].[name] = N'TotalAssetCount');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Verification].[PhysicalVerificationCycle] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Verification].[PhysicalVerificationCycle] ALTER COLUMN [TotalAssetCount] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationAssignment] (
        [PhysicalVerificationCycleId] int NOT NULL,
        [AuditorUserId] int NOT NULL,
        [AssignedOnUtc] datetime2 NOT NULL,
        [AssignedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_PhysicalVerificationAssignment] PRIMARY KEY ([PhysicalVerificationCycleId], [AuditorUserId]),
        CONSTRAINT [FK_PhysicalVerificationAssignment_Cycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    CREATE TABLE [Verification].[PhysicalVerificationCycleLocation] (
        [PhysicalVerificationCycleId] int NOT NULL,
        [BranchId] int NOT NULL,
        CONSTRAINT [PK_PhysicalVerificationCycleLocation] PRIMARY KEY ([PhysicalVerificationCycleId], [BranchId]),
        CONSTRAINT [FK_PhysicalVerificationCycleLocation_Cycle_PhysicalVerificationCycleId] FOREIGN KEY ([PhysicalVerificationCycleId]) REFERENCES [Verification].[PhysicalVerificationCycle] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_PhysicalVerificationCycle_OneActivePerBranch] ON [Verification].[PhysicalVerificationCycle] ([BranchId], [IsActive]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    CREATE INDEX [IX_PhysicalVerificationAssignment_AuditorUserId] ON [Verification].[PhysicalVerificationAssignment] ([AuditorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    CREATE INDEX [IX_PhysicalVerificationCycleLocation_BranchId] ON [Verification].[PhysicalVerificationCycleLocation] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819071519_20260819_Verification_AddAuditAssignments'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819071519_20260819_Verification_AddAuditAssignments', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819101423_20260819_Verification_AllowConcurrentBranchAudits'
)
BEGIN
    DROP INDEX [UX_PhysicalVerificationCycle_OneActivePerBranch] ON [Verification].[PhysicalVerificationCycle];
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260819101423_20260819_Verification_AllowConcurrentBranchAudits'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260819101423_20260819_Verification_AllowConcurrentBranchAudits', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [AllowedRadiusMetres] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [DistanceFromLocationMetres] decimal(12,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsAccuracyMetres] decimal(9,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationMessage] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationStatus] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [HasLocationMismatch] bit NOT NULL CONSTRAINT [DF_PhysicalVerification_HasLocationMismatch] DEFAULT (0);
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [IsMockLocation] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLatitude] decimal(9,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLongitude] decimal(9,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_AllowedRadius] CHECK (([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_Distance] CHECK (([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_GpsAccuracy] CHECK (([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_GpsValidationStatus] CHECK (([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN (N''NotValidated'', N''InsideGeofence'', N''OutsideGeofence'', N''ReferenceUnavailable'')))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLatitude] CHECK (([ReferenceLatitude] IS NULL OR ([ReferenceLatitude] >= -90 AND [ReferenceLatitude] <= 90)))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    EXEC(N'ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLongitude] CHECK (([ReferenceLongitude] IS NULL OR ([ReferenceLongitude] >= -180 AND [ReferenceLongitude] <= 180)))');
END;

IF NOT EXISTS (
    SELECT * FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824052454_20260824_Verification_AddGpsValidationEvidence', N'10.0.11');
END;

COMMIT;
GO

