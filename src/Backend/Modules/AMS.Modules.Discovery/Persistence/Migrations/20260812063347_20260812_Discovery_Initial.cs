using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Discovery.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Discovery_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Discovery");

            migrationBuilder.CreateTable(
                name: "AgentApiKey",
                schema: "Discovery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastUsedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentApiKey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetHealth",
                schema: "Discovery",
                columns: table => new
                {
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Hostname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CpuPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MemoryPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SystemDrivePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BatteryHealthPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    UptimeHours = table.Column<int>(type: "int", nullable: false),
                    LoggedInUser = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LastSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHealth", x => x.AssetId);
                });

            migrationBuilder.CreateTable(
                name: "AssetHealthHistory",
                schema: "Discovery",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CpuPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MemoryPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SystemDrivePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CapturedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHealthHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetInstalledSoftware",
                schema: "Discovery",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    SoftwareName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirstSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetInstalledSoftware", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscoveredDevice",
                schema: "Discovery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hostname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MacAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LinkedAssetId = table.Column<int>(type: "int", nullable: true),
                    FirstSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscoveredDevice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareCatalog",
                schema: "Discovery",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoftwareName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LicensedSeats = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareCatalog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentApiKey_Prefix",
                schema: "Discovery",
                table: "AgentApiKey",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_AssetHealth_LastSeen",
                schema: "Discovery",
                table: "AssetHealth",
                column: "LastSeenOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetHealthHistory_AssetTrend",
                schema: "Discovery",
                table: "AssetHealthHistory",
                columns: new[] { "AssetId", "CapturedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHealthHistory_Captured",
                schema: "Discovery",
                table: "AssetHealthHistory",
                column: "CapturedOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AssetInstalledSoftware_Name",
                schema: "Discovery",
                table: "AssetInstalledSoftware",
                column: "SoftwareName");

            migrationBuilder.CreateIndex(
                name: "UX_AssetInstalledSoftware_Install",
                schema: "Discovery",
                table: "AssetInstalledSoftware",
                columns: new[] { "AssetId", "SoftwareName", "Version" },
                unique: true,
                filter: "[Version] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DiscoveredDevice_Status",
                schema: "Discovery",
                table: "DiscoveredDevice",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_DiscoveredDevice_Machine",
                schema: "Discovery",
                table: "DiscoveredDevice",
                columns: new[] { "Hostname", "SerialNumber" },
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_SoftwareCatalog_Name",
                schema: "Discovery",
                table: "SoftwareCatalog",
                column: "SoftwareName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentApiKey",
                schema: "Discovery");

            migrationBuilder.DropTable(
                name: "AssetHealth",
                schema: "Discovery");

            migrationBuilder.DropTable(
                name: "AssetHealthHistory",
                schema: "Discovery");

            migrationBuilder.DropTable(
                name: "AssetInstalledSoftware",
                schema: "Discovery");

            migrationBuilder.DropTable(
                name: "DiscoveredDevice",
                schema: "Discovery");

            migrationBuilder.DropTable(
                name: "SoftwareCatalog",
                schema: "Discovery");
        }
    }
}
