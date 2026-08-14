using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Contracts_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Contracts");

            migrationBuilder.CreateTable(
                name: "Contract",
                schema: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ContractName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContractType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ContractValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LicensedSeats = table.Column<int>(type: "int", nullable: true),
                    LicenseKeyEncrypted = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    RenewalCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Contract", x => x.Id);
                    table.CheckConstraint("CK_Contract_Window", "([EndDate] >= [StartDate])");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ContractHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Contracts")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "ContractAsset",
                schema: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    LinkedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAsset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAsset_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "Contracts",
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractDocument",
                schema: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDocument_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "Contracts",
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractReminderLog",
                schema: "Contracts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    DaysBeforeExpiry = table.Column<int>(type: "int", nullable: false),
                    ExpiryDateSnapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    SentOnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SentTo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    EmailOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "N'Queued'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReminderLog", x => x.Id);
                    table.CheckConstraint("CK_ContractReminderLog_Outcome", "([Outcome] IN (N'Queued', N'Sent', N'Failed'))");
                    table.ForeignKey(
                        name: "FK_ContractReminderLog_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "Contracts",
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractReminderSetting",
                schema: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    DaysBeforeExpiry = table.Column<int>(type: "int", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "N'Email'"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractReminderSetting", x => x.Id);
                    table.CheckConstraint("CK_ContractReminderSetting_Channel", "([Channel] IN (N'Email', N'InApp', N'Both'))");
                    table.CheckConstraint("CK_ContractReminderSetting_Days", "([DaysBeforeExpiry] BETWEEN 1 AND 365)");
                    table.ForeignKey(
                        name: "FK_ContractReminderSetting_Contract_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "Contracts",
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contract_EndDate",
                schema: "Contracts",
                table: "Contract",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "UX_Contract_Number",
                schema: "Contracts",
                table: "Contract",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ContractAsset_NoDuplicates",
                schema: "Contracts",
                table: "ContractAsset",
                columns: new[] { "ContractId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractDocument_ContractId",
                schema: "Contracts",
                table: "ContractDocument",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "UX_ContractReminderLog_OncePerThreshold",
                schema: "Contracts",
                table: "ContractReminderLog",
                columns: new[] { "ContractId", "DaysBeforeExpiry", "ExpiryDateSnapshot" },
                unique: true,
                filter: "[Outcome] <> N'Failed'");

            migrationBuilder.CreateIndex(
                name: "UX_ContractReminderSetting_Default",
                schema: "Contracts",
                table: "ContractReminderSetting",
                column: "DaysBeforeExpiry",
                unique: true,
                filter: "[ContractId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ContractReminderSetting_PerContract",
                schema: "Contracts",
                table: "ContractReminderSetting",
                columns: new[] { "ContractId", "DaysBeforeExpiry" },
                unique: true,
                filter: "[ContractId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractAsset",
                schema: "Contracts");

            migrationBuilder.DropTable(
                name: "ContractDocument",
                schema: "Contracts");

            migrationBuilder.DropTable(
                name: "ContractReminderLog",
                schema: "Contracts");

            migrationBuilder.DropTable(
                name: "ContractReminderSetting",
                schema: "Contracts");

            migrationBuilder.DropTable(
                name: "Contract",
                schema: "Contracts")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ContractHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Contracts")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
