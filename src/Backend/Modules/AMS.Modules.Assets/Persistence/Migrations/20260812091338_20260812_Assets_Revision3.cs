using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Assets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Assets_Revision3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asset_AssetCategory_AssetCategoryId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldDefinition_AssetCategory_AssetCategoryId",
                schema: "Assets",
                table: "CustomFieldDefinition");

            // HAND-EDITED. The scaffolder emitted DropTable("AssetCategory") here
            // and CreateTable("AssetType") below, because EF cannot see a table
            // rename - it only sees one name gone and another arrived.
            //
            // Applying that as scaffolded would have destroyed every category
            // row while KEEPING Asset.AssetTypeId and
            // CustomFieldDefinition.AssetTypeId, which are the same columns
            // renamed a few lines down. Every asset would then point at a type
            // id that no longer existed, and the FKs recreated at the end of
            // this migration would fail on the first populated database.
            //
            // Revision 3 renames this table. So does the migration.
            migrationBuilder.RenameTable(
                name: "AssetCategory",
                schema: "Assets",
                newName: "AssetType",
                newSchema: "Assets");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                schema: "Assets",
                table: "AssetType",
                newName: "TypeName");

            migrationBuilder.RenameColumn(
                name: "ParentCategoryId",
                schema: "Assets",
                table: "AssetType",
                newName: "ParentAssetTypeId");

            migrationBuilder.RenameIndex(
                name: "UX_AssetCategory_Name",
                schema: "Assets",
                table: "AssetType",
                newName: "UX_AssetType_Name");

            migrationBuilder.RenameIndex(
                name: "IX_AssetCategory_ParentCategoryId",
                schema: "Assets",
                table: "AssetType",
                newName: "IX_AssetType_ParentAssetTypeId");

            // The seven behaviour flags. Every one carries a defaultValueSql so
            // the rows already in the table get a defined value rather than a
            // failed ALTER: an existing category is allocatable and physical,
            // and tracks nothing in particular, which is what it was before
            // these columns existed.
            migrationBuilder.AddColumn<bool>(
                name: "IsAllocatable",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AddColumn<bool>(
                name: "IsPhysical",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AddColumn<bool>(
                name: "IsBulkDefault",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "TracksHardware",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "TracksSoftware",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "TracksVehicle",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "TracksCalibration",
                schema: "Assets",
                table: "AssetType",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asset_CalibrationWindow",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "Make",
                schema: "Assets",
                table: "AssetHardwareDetail");

            migrationBuilder.DropColumn(
                name: "CalibrationEndDate",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "CalibrationStartDate",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.RenameColumn(
                name: "AssetCategoryId",
                schema: "Assets",
                table: "CustomFieldDefinition",
                newName: "AssetTypeId");

            migrationBuilder.RenameIndex(
                name: "UX_CustomFieldDefinition_CategoryField",
                schema: "Assets",
                table: "CustomFieldDefinition",
                newName: "UX_CustomFieldDefinition_TypeField");

            migrationBuilder.RenameColumn(
                name: "Model",
                schema: "Assets",
                table: "AssetHardwareDetail",
                newName: "Hostname");

            migrationBuilder.RenameColumn(
                name: "Hostname",
                schema: "Assets",
                table: "Asset",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "AssetCategoryId",
                schema: "Assets",
                table: "Asset",
                newName: "AssetTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Asset_AssetCategoryId",
                schema: "Assets",
                table: "Asset",
                newName: "IX_Asset_AssetTypeId");

            migrationBuilder.AddColumn<int>(
                name: "DisposalId",
                schema: "Assets",
                table: "AssetEvent",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityDelta",
                schema: "Assets",
                table: "AssetEvent",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssetClassId",
                schema: "Assets",
                table: "Asset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportBatchId",
                schema: "Assets",
                table: "Asset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBulk",
                schema: "Assets",
                table: "Asset",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<string>(
                name: "Make",
                schema: "Assets",
                table: "Asset",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                schema: "Assets",
                table: "Asset",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AddColumn<int>(
                name: "SplitFromAssetId",
                schema: "Assets",
                table: "Asset",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                schema: "Assets",
                table: "Asset",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetClass",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportingCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDepreciable = table.Column<bool>(type: "bit", nullable: false),
                    IsIntangible = table.Column<bool>(type: "bit", nullable: false),
                    IsAuc = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetClass", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetDepreciationEntry",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    FinancialYear = table.Column<short>(type: "smallint", nullable: false),
                    OpeningAccumulated = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Additions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChargedForPeriod = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingAccumulated = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetBookValueAtClose = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SyncedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDepreciationEntry", x => x.Id);
                    table.CheckConstraint("CK_AssetDepreciationEntry_Source", "([SourceSystem] IN (N'Sap', N'Import'))");
                    table.ForeignKey(
                        name: "FK_AssetDepreciationEntry_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetDisposal",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    DisposalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DisposalQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    DisposalGrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SaleProceeds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DisposalReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDisposal", x => x.Id);
                    table.CheckConstraint("CK_AssetDisposal_QuantityPositive", "([DisposalQuantity] > 0)");
                    table.ForeignKey(
                        name: "FK_AssetDisposal_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetHolding",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    CustomerSiteId = table.Column<int>(type: "int", nullable: true),
                    OnHandQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHolding", x => x.Id);
                    table.CheckConstraint("CK_AssetHolding_NonNegative", "([OnHandQuantity] >= 0)");
                    table.CheckConstraint("CK_AssetHolding_OnePlaceKind", "(([LocationId] IS NOT NULL AND [CustomerSiteId] IS NULL) OR ([LocationId] IS NULL AND [CustomerSiteId] IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_AssetHolding_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetInstrumentDetail",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CalibrationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalibrationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalibrationFrequencyMonths = table.Column<int>(type: "int", nullable: true),
                    CalibrationAgency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    MeasurementRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccuracyClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetInstrumentDetail", x => x.AssetId);
                    table.CheckConstraint("CK_AssetInstrumentDetail_Window", "([CalibrationEndDate] IS NULL OR [CalibrationStartDate] IS NULL OR [CalibrationEndDate] >= [CalibrationStartDate])");
                    table.ForeignKey(
                        name: "FK_AssetInstrumentDetail_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // AssetType is NOT created here - it is the renamed AssetCategory,
            // handled at the top of this method. sp_rename leaves the PRIMARY KEY
            // and the self-referencing FOREIGN KEY carrying their old names, and
            // a constraint whose name no longer matches its table is exactly the
            // drift Compare-Schema.ps1 exists to catch, so rename them too.
            migrationBuilder.Sql(
                "EXEC sp_rename N'[Assets].[PK_AssetCategory]', N'PK_AssetType', N'OBJECT';");
            migrationBuilder.Sql(
                "EXEC sp_rename N'[Assets].[FK_AssetCategory_AssetCategory_ParentCategoryId]', "
                + "N'FK_AssetType_AssetType_ParentAssetTypeId', N'OBJECT';");

            migrationBuilder.CreateTable(
                name: "AssetVehicleDetail",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChassisNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EngineNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FitnessExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PucExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InsuranceExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OdometerKm = table.Column<int>(type: "int", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetVehicleDetail", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_AssetVehicleDetail_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccount",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoaCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartOfAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetFinance",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    OriginalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MigratedBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AdditionalValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DisposalGrossValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NetBookValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DepreciationMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DepreciationPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: true),
                    CapitalisedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    FirstAcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SapPostingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AucReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OpportunityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ApVoucherNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    GrossValueCoaId = table.Column<int>(type: "int", nullable: true),
                    AccumulatedDepreciationCoaId = table.Column<int>(type: "int", nullable: true),
                    DepreciationChargeCoaId = table.Column<int>(type: "int", nullable: true),
                    LastSyncedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFinance", x => x.AssetId);
                    table.CheckConstraint("CK_AssetFinance_Method", "([DepreciationMethod] IS NULL OR [DepreciationMethod] IN (N'StraightLine', N'WrittenDownValue', N'None'))");
                    table.ForeignKey(
                        name: "FK_AssetFinance_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetFinance_ChartOfAccount_AccumulatedDepreciationCoaId",
                        column: x => x.AccumulatedDepreciationCoaId,
                        principalSchema: "Assets",
                        principalTable: "ChartOfAccount",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssetFinance_ChartOfAccount_DepreciationChargeCoaId",
                        column: x => x.DepreciationChargeCoaId,
                        principalSchema: "Assets",
                        principalTable: "ChartOfAccount",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssetFinance_ChartOfAccount_GrossValueCoaId",
                        column: x => x.GrossValueCoaId,
                        principalSchema: "Assets",
                        principalTable: "ChartOfAccount",
                        principalColumn: "Id");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition",
                sql: "([FieldType] IN (N'Text', N'Number', N'Percentage', N'Date', N'Boolean', N'Dropdown'))");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_AssetClassId",
                schema: "Assets",
                table: "Asset",
                column: "AssetClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset",
                column: "CapitalisedFromAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_ImportBatchId",
                schema: "Assets",
                table: "Asset",
                column: "ImportBatchId",
                filter: "[ImportBatchId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asset_BulkHasUom",
                schema: "Assets",
                table: "Asset",
                sql: "([IsBulk] = 0 OR [UnitOfMeasure] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asset_BulkNotHeld",
                schema: "Assets",
                table: "Asset",
                sql: "([IsBulk] = 0 OR ([CurrentEmployeeId] IS NULL AND [CurrentLocationId] IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asset_QuantityPositive",
                schema: "Assets",
                table: "Asset",
                sql: "([Quantity] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asset_UnitQuantityIsOne",
                schema: "Assets",
                table: "Asset",
                sql: "([IsBulk] = 1 OR [Quantity] = 1)");

            migrationBuilder.CreateIndex(
                name: "UX_AssetClass_Code",
                schema: "Assets",
                table: "AssetClass",
                column: "ClassCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AssetClass_Name",
                schema: "Assets",
                table: "AssetClass",
                column: "ClassName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AssetClass_OneAuc",
                schema: "Assets",
                table: "AssetClass",
                column: "IsAuc",
                unique: true,
                filter: "[IsAuc] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_AssetDepreciationEntry_AssetYear",
                schema: "Assets",
                table: "AssetDepreciationEntry",
                columns: new[] { "AssetId", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetDisposal_AssetId",
                schema: "Assets",
                table: "AssetDisposal",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetHolding_Location",
                schema: "Assets",
                table: "AssetHolding",
                column: "LocationId",
                filter: "[OnHandQuantity] > 0");

            migrationBuilder.CreateIndex(
                name: "UX_AssetHolding_AssetLocation",
                schema: "Assets",
                table: "AssetHolding",
                columns: new[] { "AssetId", "LocationId" },
                unique: true,
                filter: "[LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AssetHolding_AssetSite",
                schema: "Assets",
                table: "AssetHolding",
                columns: new[] { "AssetId", "CustomerSiteId" },
                unique: true,
                filter: "[CustomerSiteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetInstrumentDetail_CalibrationDue",
                schema: "Assets",
                table: "AssetInstrumentDetail",
                column: "CalibrationEndDate");

            // The two AssetType indexes are NOT created here: the scaffolder
            // thought this was a new table, but they arrived with the rename at
            // the top of this method and creating them again fails with 1913.

            migrationBuilder.CreateIndex(
                name: "UX_AssetVehicleDetail_Registration",
                schema: "Assets",
                table: "AssetVehicleDetail",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ChartOfAccount_Code",
                schema: "Assets",
                table: "ChartOfAccount",
                column: "CoaCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_AssetClass_AssetClassId",
                schema: "Assets",
                table: "Asset",
                column: "AssetClassId",
                principalSchema: "Assets",
                principalTable: "AssetClass",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_AssetType_AssetTypeId",
                schema: "Assets",
                table: "Asset",
                column: "AssetTypeId",
                principalSchema: "Assets",
                principalTable: "AssetType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Asset_CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset",
                column: "CapitalisedFromAssetId",
                principalSchema: "Assets",
                principalTable: "Asset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_Asset_SplitFromAssetId",
                schema: "Assets",
                table: "Asset",
                column: "SplitFromAssetId",
                principalSchema: "Assets",
                principalTable: "Asset",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldDefinition_AssetType_AssetTypeId",
                schema: "Assets",
                table: "CustomFieldDefinition",
                column: "AssetTypeId",
                principalSchema: "Assets",
                principalTable: "AssetType",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Asset_AssetClass_AssetClassId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Asset_AssetType_AssetTypeId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Asset_Asset_CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_Asset_Asset_SplitFromAssetId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomFieldDefinition_AssetType_AssetTypeId",
                schema: "Assets",
                table: "CustomFieldDefinition");

            migrationBuilder.DropTable(
                name: "AssetClass",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetDepreciationEntry",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetDisposal",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetFinance",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetHolding",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetInstrumentDetail",
                schema: "Assets");

            // HAND-EDITED, mirroring Up(). AssetType is not dropped - it is
            // renamed back to AssetCategory further down, so that reverting this
            // migration keeps the category rows it started with.

            migrationBuilder.DropTable(
                name: "AssetVehicleDetail",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "ChartOfAccount",
                schema: "Assets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition");

            migrationBuilder.DropIndex(
                name: "IX_Asset_AssetClassId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Asset_CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropIndex(
                name: "IX_Asset_ImportBatchId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asset_BulkHasUom",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asset_BulkNotHeld",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asset_QuantityPositive",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asset_UnitQuantityIsOne",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "DisposalId",
                schema: "Assets",
                table: "AssetEvent");

            migrationBuilder.DropColumn(
                name: "QuantityDelta",
                schema: "Assets",
                table: "AssetEvent");

            migrationBuilder.DropColumn(
                name: "AssetClassId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "CapitalisedFromAssetId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "ImportBatchId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "IsBulk",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "Make",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "SplitFromAssetId",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                schema: "Assets",
                table: "Asset");

            migrationBuilder.RenameColumn(
                name: "AssetTypeId",
                schema: "Assets",
                table: "CustomFieldDefinition",
                newName: "AssetCategoryId");

            migrationBuilder.RenameIndex(
                name: "UX_CustomFieldDefinition_TypeField",
                schema: "Assets",
                table: "CustomFieldDefinition",
                newName: "UX_CustomFieldDefinition_CategoryField");

            migrationBuilder.RenameColumn(
                name: "Hostname",
                schema: "Assets",
                table: "AssetHardwareDetail",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "Model",
                schema: "Assets",
                table: "Asset",
                newName: "Hostname");

            migrationBuilder.RenameColumn(
                name: "AssetTypeId",
                schema: "Assets",
                table: "Asset",
                newName: "AssetCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Asset_AssetTypeId",
                schema: "Assets",
                table: "Asset",
                newName: "IX_Asset_AssetCategoryId");

            migrationBuilder.AddColumn<string>(
                name: "Make",
                schema: "Assets",
                table: "AssetHardwareDetail",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CalibrationEndDate",
                schema: "Assets",
                table: "Asset",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CalibrationStartDate",
                schema: "Assets",
                table: "Asset",
                type: "date",
                nullable: true);

            // HAND-EDITED, mirroring Up(): drop the seven flags, then rename the
            // table, its two renamed columns and its constraints back.
            foreach (var flag in new[]
            {
                "IsAllocatable", "IsPhysical", "IsBulkDefault", "TracksHardware",
                "TracksSoftware", "TracksVehicle", "TracksCalibration",
            })
            {
                migrationBuilder.DropColumn(name: flag, schema: "Assets", table: "AssetType");
            }

            migrationBuilder.Sql(
                "EXEC sp_rename N'[Assets].[FK_AssetType_AssetType_ParentAssetTypeId]', "
                + "N'FK_AssetCategory_AssetCategory_ParentCategoryId', N'OBJECT';");
            migrationBuilder.Sql(
                "EXEC sp_rename N'[Assets].[PK_AssetType]', N'PK_AssetCategory', N'OBJECT';");

            migrationBuilder.RenameIndex(
                name: "IX_AssetType_ParentAssetTypeId",
                schema: "Assets",
                table: "AssetType",
                newName: "IX_AssetCategory_ParentCategoryId");

            migrationBuilder.RenameIndex(
                name: "UX_AssetType_Name",
                schema: "Assets",
                table: "AssetType",
                newName: "UX_AssetCategory_Name");

            migrationBuilder.RenameColumn(
                name: "ParentAssetTypeId",
                schema: "Assets",
                table: "AssetType",
                newName: "ParentCategoryId");

            migrationBuilder.RenameColumn(
                name: "TypeName",
                schema: "Assets",
                table: "AssetType",
                newName: "CategoryName");

            migrationBuilder.RenameTable(
                name: "AssetType",
                schema: "Assets",
                newName: "AssetCategory",
                newSchema: "Assets");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition",
                sql: "[FieldType] IN (N'Text', N'Number', N'Percentage', N'Date', N'Boolean', N'Dropdown')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asset_CalibrationWindow",
                schema: "Assets",
                table: "Asset",
                sql: "([CalibrationEndDate] IS NULL OR [CalibrationStartDate] IS NULL OR [CalibrationEndDate] >= [CalibrationStartDate])");

            // The two AssetCategory indexes are NOT created here: they were
            // renamed rather than dropped, a few lines above.

            migrationBuilder.AddForeignKey(
                name: "FK_Asset_AssetCategory_AssetCategoryId",
                schema: "Assets",
                table: "Asset",
                column: "AssetCategoryId",
                principalSchema: "Assets",
                principalTable: "AssetCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomFieldDefinition_AssetCategory_AssetCategoryId",
                schema: "Assets",
                table: "CustomFieldDefinition",
                column: "AssetCategoryId",
                principalSchema: "Assets",
                principalTable: "AssetCategory",
                principalColumn: "Id");
        }
    }
}
