USE [AMS];
GO

SET XACT_ABORT ON;
GO

IF COL_LENGTH(N'Verification.PhysicalVerification', N'AllowedRadiusMetres') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [AllowedRadiusMetres] decimal(12,2) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'DistanceFromLocationMetres') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [DistanceFromLocationMetres] decimal(12,2) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'GpsAccuracyMetres') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsAccuracyMetres] decimal(9,2) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'GpsValidationMessage') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationMessage] nvarchar(500) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'GpsValidationStatus') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationStatus] nvarchar(20) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'HasLocationMismatch') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD [HasLocationMismatch] bit NOT NULL
            CONSTRAINT [DF_PhysicalVerification_HasLocationMismatch] DEFAULT (0);
IF COL_LENGTH(N'Verification.PhysicalVerification', N'IsMockLocation') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [IsMockLocation] bit NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'ReferenceLatitude') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLatitude] decimal(9,6) NULL;
IF COL_LENGTH(N'Verification.PhysicalVerification', N'ReferenceLongitude') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLongitude] decimal(9,6) NULL;
GO

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_AllowedRadius]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_AllowedRadius]
        CHECK ([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0);

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_Distance]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_Distance]
        CHECK ([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0);

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_GpsAccuracy]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_GpsAccuracy]
        CHECK ([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0);

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_GpsValidationStatus]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_GpsValidationStatus]
        CHECK ([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN
            (N'NotValidated', N'InsideGeofence', N'OutsideGeofence', N'ReferenceUnavailable'));

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_ReferenceLatitude]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLatitude]
        CHECK ([ReferenceLatitude] IS NULL OR [ReferenceLatitude] BETWEEN -90 AND 90);

IF OBJECT_ID(N'[Verification].[CK_PhysicalVerification_ReferenceLongitude]', N'C') IS NULL
    ALTER TABLE [Verification].[PhysicalVerification]
        ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLongitude]
        CHECK ([ReferenceLongitude] IS NULL OR [ReferenceLongitude] BETWEEN -180 AND 180);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM [Verification].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260824052454_20260824_Verification_AddGpsValidationEvidence'
)
BEGIN
    INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260824052454_20260824_Verification_AddGpsValidationEvidence', N'10.0.11');
END;
GO

SELECT
    COL_LENGTH(N'Verification.PhysicalVerification', N'GpsAccuracyMetres') AS GpsAccuracyMetres,
    COL_LENGTH(N'Verification.PhysicalVerification', N'ReferenceLatitude') AS ReferenceLatitude,
    COL_LENGTH(N'Verification.PhysicalVerification', N'ReferenceLongitude') AS ReferenceLongitude,
    COL_LENGTH(N'Verification.PhysicalVerification', N'DistanceFromLocationMetres') AS DistanceFromLocationMetres,
    COL_LENGTH(N'Verification.PhysicalVerification', N'AllowedRadiusMetres') AS AllowedRadiusMetres,
    COL_LENGTH(N'Verification.PhysicalVerification', N'GpsValidationStatus') AS GpsValidationStatus,
    COL_LENGTH(N'Verification.PhysicalVerification', N'HasLocationMismatch') AS HasLocationMismatch,
    COL_LENGTH(N'Verification.PhysicalVerification', N'IsMockLocation') AS IsMockLocation,
    COL_LENGTH(N'Verification.PhysicalVerification', N'GpsValidationMessage') AS GpsValidationMessage;
GO
