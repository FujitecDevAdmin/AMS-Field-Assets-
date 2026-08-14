SET NOCOUNT ON;
DECLARE @pass int = 0, @fail int = 0;
DECLARE @typeId int, @classId int, @statusId int, @assetId int, @bulkId int;

PRINT N'--- seeds ---';
SELECT N'AssetClass rows' = COUNT(*) FROM [Assets].[AssetClass];
SELECT N'AssetStatus rows' = COUNT(*) FROM [Assets].[AssetStatus];
SELECT N'AUC classes (must be 1)' = COUNT(*) FROM [Assets].[AssetClass] WHERE [IsAuc] = 1;
SELECT N'distinct reporting categories (must be 9)' = COUNT(DISTINCT [ReportingCategory]) FROM [Assets].[AssetClass];

INSERT INTO [Assets].[AssetType] ([TypeName], [IsActive], [CreatedOnUtc]) VALUES (N'Probe Unit', 1, SYSUTCDATETIME());
SET @typeId = SCOPE_IDENTITY();
SELECT TOP 1 @classId = [Id] FROM [Assets].[AssetClass] WHERE [ClassCode] = N'F & F';
SELECT TOP 1 @statusId = [Id] FROM [Assets].[AssetStatus] WHERE [StatusName] = N'In Stock';

PRINT N'--- CK_AssetClass_OneAuc: a second AUC class must be rejected ---';
BEGIN TRY
    INSERT INTO [Assets].[AssetClass] ([ClassCode],[ClassName],[ReportingCategory],[IsDepreciable],[IsIntangible],[IsAuc],[IsActive],[CreatedOnUtc])
    VALUES (N'AUC2', N'Second AUC', N'AUC', 0, 0, 1, 1, SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - a second AUC class was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected, error ' + CAST(ERROR_NUMBER() AS nvarchar(10)) + N' (2601 = the filtered unique index)';
END CATCH

PRINT N'--- CK_Asset_UnitQuantityIsOne: a NON-bulk asset with Quantity 5 must be rejected ---';
BEGIN TRY
    INSERT INTO [Assets].[Asset] ([AssetNumber],[AssetName],[AssetTypeId],[AssetClassId],[AssetStatusId],[IsBulk],[Quantity],[IsDeleted],[CreatedOnUtc],[ConcurrencyStamp])
    VALUES (N'PROBE-BADQTY', N'Bad', @typeId, @classId, @statusId, 0, 5, 0, SYSUTCDATETIME(), NEWID());
    SET @fail += 1; PRINT N'  FAIL - a unit asset with Quantity 5 was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected by ' + ISNULL(ERROR_MESSAGE(), N'?');
END CATCH

PRINT N'--- CK_Asset_BulkHasUom: a bulk asset with no unit of measure must be rejected ---';
BEGIN TRY
    INSERT INTO [Assets].[Asset] ([AssetNumber],[AssetName],[AssetTypeId],[AssetClassId],[AssetStatusId],[IsBulk],[Quantity],[IsDeleted],[CreatedOnUtc],[ConcurrencyStamp])
    VALUES (N'PROBE-NOUOM', N'Bad', @typeId, @classId, @statusId, 1, 10, 0, SYSUTCDATETIME(), NEWID());
    SET @fail += 1; PRINT N'  FAIL - a bulk asset with no UoM was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected';
END CATCH

PRINT N'--- the happy paths must still work ---';
BEGIN TRY
    INSERT INTO [Assets].[Asset] ([AssetNumber],[AssetName],[AssetTypeId],[AssetClassId],[AssetStatusId],[IsBulk],[Quantity],[IsDeleted],[CreatedOnUtc],[ConcurrencyStamp])
    VALUES (N'PROBE-UNIT', N'A laptop', @typeId, @classId, @statusId, 0, 1, 0, SYSUTCDATETIME(), NEWID());
    SET @assetId = SCOPE_IDENTITY();
    INSERT INTO [Assets].[Asset] ([AssetNumber],[AssetName],[AssetTypeId],[AssetClassId],[AssetStatusId],[IsBulk],[Quantity],[UnitOfMeasure],[IsDeleted],[CreatedOnUtc],[ConcurrencyStamp])
    VALUES (N'PROBE-BULK', N'495 barricades', @typeId, @classId, @statusId, 1, 495, N'Nos', 0, SYSUTCDATETIME(), NEWID());
    SET @bulkId = SCOPE_IDENTITY();
    SET @pass += 1; PRINT N'  pass - a unit asset and a 495-strong bulk line both inserted';
END TRY BEGIN CATCH
    SET @fail += 1; PRINT N'  FAIL - ' + ERROR_MESSAGE();
END CATCH

PRINT N'--- CK_AssetHolding_NonNegative: over-issue must die in the database ---';
INSERT INTO [Assets].[AssetHolding] ([AssetId],[LocationId],[OnHandQuantity],[CreatedOnUtc]) VALUES (@bulkId, 1, 20, SYSUTCDATETIME());
BEGIN TRY
    UPDATE [Assets].[AssetHolding] SET [OnHandQuantity] = [OnHandQuantity] - 25 WHERE [AssetId] = @bulkId AND [LocationId] = 1;
    SET @fail += 1; PRINT N'  FAIL - issuing 25 from a holding of 20 was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected, error ' + CAST(ERROR_NUMBER() AS nvarchar(10)) + N' (547 = the CHECK)';
END CATCH

PRINT N'--- UX_AssetHolding_AssetLocation: two balances for one asset at one place ---';
BEGIN TRY
    INSERT INTO [Assets].[AssetHolding] ([AssetId],[LocationId],[OnHandQuantity],[CreatedOnUtc]) VALUES (@bulkId, 1, 7, SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - a duplicate holding row was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected, error ' + CAST(ERROR_NUMBER() AS nvarchar(10));
END CATCH

PRINT N'--- but the SAME asset at a DIFFERENT place is correct, not a duplicate ---';
BEGIN TRY
    INSERT INTO [Assets].[AssetHolding] ([AssetId],[LocationId],[OnHandQuantity],[CreatedOnUtc]) VALUES (@bulkId, 2, 7, SYSUTCDATETIME());
    SET @pass += 1; PRINT N'  pass - accepted';
END TRY BEGIN CATCH
    SET @fail += 1; PRINT N'  FAIL - ' + ERROR_MESSAGE();
END CATCH

PRINT N'--- CK_AssetHolding_OnePlaceKind: a holding at both a branch and a site ---';
BEGIN TRY
    INSERT INTO [Assets].[AssetHolding] ([AssetId],[LocationId],[CustomerSiteId],[OnHandQuantity],[CreatedOnUtc]) VALUES (@bulkId, 3, 3, 1, SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - a holding in two kinds of place was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - rejected';
END CATCH

PRINT N'--- verification: the split indexes ---';
INSERT INTO [Verification].[PhysicalVerificationCycle] ([CycleName],[StartDate],[IsActive],[CreatedOnUtc])
VALUES (N'Probe cycle', '2026-08-01', 1, SYSUTCDATETIME());
DECLARE @cycleId int = SCOPE_IDENTITY();
INSERT INTO [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId],[AssetId],[HasQrMismatch],[WorkingCondition],[SerialVerified],[LocationId],[VerifiedByUserId],[VerifiedOnUtc],[CreatedOnUtc])
VALUES (@cycleId, @assetId, 0, N'Good', 1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
BEGIN TRY
    INSERT INTO [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId],[AssetId],[HasQrMismatch],[WorkingCondition],[SerialVerified],[LocationId],[VerifiedByUserId],[VerifiedOnUtc],[CreatedOnUtc])
    VALUES (@cycleId, @assetId, 0, N'Good', 1, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - a unit asset was sighted twice in one cycle';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - second sighting of a UNIT asset rejected, error ' + CAST(ERROR_NUMBER() AS nvarchar(10));
END CATCH
BEGIN TRY
    INSERT INTO [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId],[AssetId],[IsBulkCount],[CountedQuantity],[HasQrMismatch],[WorkingCondition],[SerialVerified],[LocationId],[VerifiedByUserId],[VerifiedOnUtc],[CreatedOnUtc])
    VALUES (@cycleId, @bulkId, 1, 20, 0, N'Good', 0, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
           (@cycleId, @bulkId, 1,  7, 0, N'Good', 0, 2, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @pass += 1; PRINT N'  pass - the SAME bulk line counted at two branches, both accepted';
END TRY BEGIN CATCH
    SET @fail += 1; PRINT N'  FAIL - ' + ERROR_MESSAGE();
END CATCH
BEGIN TRY
    INSERT INTO [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId],[AssetId],[IsBulkCount],[CountedQuantity],[HasQrMismatch],[WorkingCondition],[SerialVerified],[LocationId],[VerifiedByUserId],[VerifiedOnUtc],[CreatedOnUtc])
    VALUES (@cycleId, @bulkId, 1, 3, 0, N'Good', 0, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - the same bulk line was counted twice at ONE branch';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - double count at one branch rejected, error ' + CAST(ERROR_NUMBER() AS nvarchar(10));
END CATCH
BEGIN TRY
    INSERT INTO [Verification].[PhysicalVerification] ([PhysicalVerificationCycleId],[AssetId],[IsBulkCount],[HasQrMismatch],[WorkingCondition],[SerialVerified],[LocationId],[VerifiedByUserId],[VerifiedOnUtc],[CreatedOnUtc])
    VALUES (@cycleId, @bulkId, 1, 0, N'Good', 0, 9, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
    SET @fail += 1; PRINT N'  FAIL - a bulk count with no number was accepted';
END TRY BEGIN CATCH
    SET @pass += 1; PRINT N'  pass - a bulk count with no CountedQuantity rejected';
END CATCH

PRINT N'--- AssetMovement.Quantity defaults to 1 for existing callers ---';
INSERT INTO [Movements].[AssetMovement] ([AssetId],[MovementType],[FromLocationId],[ToLocationId],[Status],[ShippedOnUtc],[CreatedOnUtc])
VALUES (@assetId, N'Transfer', 1, 2, N'InTransit', SYSUTCDATETIME(), SYSUTCDATETIME());
IF (SELECT [Quantity] FROM [Movements].[AssetMovement] WHERE [AssetId] = @assetId) = 1
    BEGIN SET @pass += 1; PRINT N'  pass - defaulted to 1'; END
ELSE BEGIN SET @fail += 1; PRINT N'  FAIL - did not default to 1'; END

PRINT N'';
PRINT N'==================== ' + CAST(@pass AS nvarchar(10)) + N' passed, ' + CAST(@fail AS nvarchar(10)) + N' failed ====================';
IF @fail > 0 RAISERROR(N'probe failures', 16, 1);
