using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Assets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Assets_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Assets");

            migrationBuilder.CreateTable(
                name: "AssetCategory",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCategory_AssetCategory_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "Assets",
                        principalTable: "AssetCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetStatus",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinition",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValidationRegex = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinition", x => x.Id);
                    table.CheckConstraint("CK_CustomFieldDefinition_Range", "([MinValue] IS NULL OR [MaxValue] IS NULL OR [MaxValue] >= [MinValue])");
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinition_AssetCategory_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "Assets",
                        principalTable: "AssetCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Asset",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Hostname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    AssetStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentLocationId = table.Column<int>(type: "int", nullable: true),
                    CurrentEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    CostCenter = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    AcquisitionDate = table.Column<DateOnly>(type: "date", nullable: true),
                    QrCodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BarcodeValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErpAssetNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SapAssetNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SapAssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SapPlant = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LastSapSyncOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalibrationStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CalibrationEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastPhysicalCheckOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                    table.CheckConstraint("CK_Asset_CalibrationWindow", "([CalibrationEndDate] IS NULL OR [CalibrationStartDate] IS NULL OR [CalibrationEndDate] >= [CalibrationStartDate])");
                    table.ForeignKey(
                        name: "FK_Asset_AssetCategory_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "Assets",
                        principalTable: "AssetCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Asset_AssetStatus_AssetStatusId",
                        column: x => x.AssetStatusId,
                        principalSchema: "Assets",
                        principalTable: "AssetStatus",
                        principalColumn: "Id");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AssetHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Assets")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "CustomFieldOption",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    OptionValue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldOption_CustomFieldDefinition_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalSchema: "Assets",
                        principalTable: "CustomFieldDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetEvent",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    EmployeeNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    LocationNameSnapshot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AllocationId = table.Column<int>(type: "int", nullable: true),
                    MovementId = table.Column<int>(type: "int", nullable: true),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    HandoverId = table.Column<int>(type: "int", nullable: true),
                    VerificationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetEvent_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetHardwareDetail",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChassisType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Processor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MemoryGb = table.Column<int>(type: "int", nullable: true),
                    StorageGb = table.Column<int>(type: "int", nullable: true),
                    MonitorModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MonitorSerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHardwareDetail", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_AssetHardwareDetail_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetPurchaseDetail",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    PurchaseOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WarrantyStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPurchaseDetail", x => x.AssetId);
                    table.CheckConstraint("CK_AssetPurchaseDetail_WarrantyWindow", "([WarrantyEndDate] IS NULL OR [WarrantyStartDate] IS NULL OR [WarrantyEndDate] >= [WarrantyStartDate])");
                    table.ForeignKey(
                        name: "FK_AssetPurchaseDetail_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetSoftwareDetail",
                schema: "Assets",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    OperatingSystem = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OperatingSystemBuild = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Architecture = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OfficeVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Antivirus = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OsKeyEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSoftwareDetail", x => x.AssetId);
                    table.ForeignKey(
                        name: "FK_AssetSoftwareDetail_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetCustomValue",
                schema: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OptionId = table.Column<int>(type: "int", nullable: true),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCustomValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCustomValue_Asset_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "Assets",
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetCustomValue_CustomFieldDefinition_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalSchema: "Assets",
                        principalTable: "CustomFieldDefinition",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssetCustomValue_CustomFieldOption_OptionId",
                        column: x => x.OptionId,
                        principalSchema: "Assets",
                        principalTable: "CustomFieldOption",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_AssetCategoryId",
                schema: "Assets",
                table: "Asset",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_AssetStatusId",
                schema: "Assets",
                table: "Asset",
                column: "AssetStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_LocationStatus",
                schema: "Assets",
                table: "Asset",
                columns: new[] { "CurrentLocationId", "AssetStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Asset_Serial",
                schema: "Assets",
                table: "Asset",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "UX_Asset_Number",
                schema: "Assets",
                table: "Asset",
                column: "AssetNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Asset_QrCode",
                schema: "Assets",
                table: "Asset",
                column: "QrCodeValue",
                unique: true,
                filter: "[QrCodeValue] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Asset_SapNumber",
                schema: "Assets",
                table: "Asset",
                column: "SapAssetNumber",
                unique: true,
                filter: "[SapAssetNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategory_ParentCategoryId",
                schema: "Assets",
                table: "AssetCategory",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetCategory_Name",
                schema: "Assets",
                table: "AssetCategory",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetCustomValue_NumericLookup",
                schema: "Assets",
                table: "AssetCustomValue",
                columns: new[] { "CustomFieldDefinitionId", "ValueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetCustomValue_OptionId",
                schema: "Assets",
                table: "AssetCustomValue",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetCustomValue_AssetField",
                schema: "Assets",
                table: "AssetCustomValue",
                columns: new[] { "AssetId", "CustomFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvent_Asset",
                schema: "Assets",
                table: "AssetEvent",
                columns: new[] { "AssetId", "EventOnUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetStatus_Name",
                schema: "Assets",
                table: "AssetStatus",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CustomFieldDefinition_CategoryField",
                schema: "Assets",
                table: "CustomFieldDefinition",
                columns: new[] { "AssetCategoryId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CustomFieldOption_Value",
                schema: "Assets",
                table: "CustomFieldOption",
                columns: new[] { "CustomFieldDefinitionId", "OptionValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetCustomValue",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetEvent",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetHardwareDetail",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetPurchaseDetail",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetSoftwareDetail",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "CustomFieldOption",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "Asset",
                schema: "Assets")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AssetHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Assets")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinition",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetStatus",
                schema: "Assets");

            migrationBuilder.DropTable(
                name: "AssetCategory",
                schema: "Assets");
        }
    }
}
