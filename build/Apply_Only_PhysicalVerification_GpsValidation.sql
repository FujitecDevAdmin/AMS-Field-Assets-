BEGIN TRANSACTION;
ALTER TABLE [Verification].[PhysicalVerification] ADD [AllowedRadiusMetres] decimal(12,2) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [DistanceFromLocationMetres] decimal(12,2) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsAccuracyMetres] decimal(9,2) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationMessage] nvarchar(500) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [GpsValidationStatus] nvarchar(20) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [HasLocationMismatch] bit NOT NULL CONSTRAINT [DF_PhysicalVerification_HasLocationMismatch] DEFAULT (0);

ALTER TABLE [Verification].[PhysicalVerification] ADD [IsMockLocation] bit NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLatitude] decimal(9,6) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD [ReferenceLongitude] decimal(9,6) NULL;

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_AllowedRadius] CHECK (([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0));

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_Distance] CHECK (([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0));

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_GpsAccuracy] CHECK (([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0));

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_GpsValidationStatus] CHECK (([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN (N'NotValidated', N'InsideGeofence', N'OutsideGeofence', N'ReferenceUnavailable')));

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLatitude] CHECK (([ReferenceLatitude] IS NULL OR ([ReferenceLatitude] >= -90 AND [ReferenceLatitude] <= 90)));

ALTER TABLE [Verification].[PhysicalVerification] ADD CONSTRAINT [CK_PhysicalVerification_ReferenceLongitude] CHECK (([ReferenceLongitude] IS NULL OR ([ReferenceLongitude] >= -180 AND [ReferenceLongitude] <= 180)));

INSERT INTO [Verification].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260824052454_20260824_Verification_AddGpsValidationEvidence', N'10.0.11');

COMMIT;
GO

