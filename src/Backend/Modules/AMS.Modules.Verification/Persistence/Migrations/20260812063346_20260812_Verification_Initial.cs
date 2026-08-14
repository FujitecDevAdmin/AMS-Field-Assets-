using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Verification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Verification_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Verification");

            migrationBuilder.CreateTable(
                name: "PhysicalVerificationCycle",
                schema: "Verification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClosedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVerificationCycle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhysicalVerification",
                schema: "Verification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhysicalVerificationCycleId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    ClientCaptureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScannedQrValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HasQrMismatch = table.Column<bool>(type: "bit", nullable: false),
                    WorkingCondition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SerialVerified = table.Column<bool>(type: "bit", nullable: false),
                    GpsLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    GpsLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    HolderEmployeeId = table.Column<int>(type: "int", nullable: true),
                    StatusUpdatedToId = table.Column<int>(type: "int", nullable: true),
                    VerifiedByUserId = table.Column<int>(type: "int", nullable: false),
                    VerifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVerification", x => x.Id);
                    table.CheckConstraint("CK_PhysicalVerification_Condition", "([WorkingCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing'))");
                    table.ForeignKey(
                        name: "FK_PhysicalVerification_PhysicalVerificationCycle_PhysicalVerificationCycleId",
                        column: x => x.PhysicalVerificationCycleId,
                        principalSchema: "Verification",
                        principalTable: "PhysicalVerificationCycle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVerification_Exceptions",
                schema: "Verification",
                table: "PhysicalVerification",
                columns: new[] { "LocationId", "WorkingCondition" });

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerification_ClientCapture",
                schema: "Verification",
                table: "PhysicalVerification",
                column: "ClientCaptureId",
                unique: true,
                filter: "[ClientCaptureId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerification_OnePerAssetPerCycle",
                schema: "Verification",
                table: "PhysicalVerification",
                columns: new[] { "PhysicalVerificationCycleId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerificationCycle_Name",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                column: "CycleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerificationCycle_OneActive",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhysicalVerification",
                schema: "Verification");

            migrationBuilder.DropTable(
                name: "PhysicalVerificationCycle",
                schema: "Verification");
        }
    }
}
