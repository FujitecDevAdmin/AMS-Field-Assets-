using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Verification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260819_Verification_AddAuditAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerificationCycle_OneActive",
                schema: "Verification",
                table: "PhysicalVerificationCycle");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalAssetCount",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Verification].[PhysicalVerificationCycle] SET [BranchId] = 0, [TotalAssetCount] = 0;");

            migrationBuilder.AlterColumn<int>(
                name: "BranchId", schema: "Verification", table: "PhysicalVerificationCycle",
                type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalAssetCount", schema: "Verification", table: "PhysicalVerificationCycle",
                type: "int", nullable: false,
                oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PhysicalVerificationAssignment",
                schema: "Verification",
                columns: table => new
                {
                    PhysicalVerificationCycleId = table.Column<int>(type: "int", nullable: false),
                    AuditorUserId = table.Column<int>(type: "int", nullable: false),
                    AssignedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVerificationAssignment", x => new { x.PhysicalVerificationCycleId, x.AuditorUserId });
                    table.ForeignKey(
                        name: "FK_PhysicalVerificationAssignment_Cycle_PhysicalVerificationCycleId",
                        column: x => x.PhysicalVerificationCycleId,
                        principalSchema: "Verification",
                        principalTable: "PhysicalVerificationCycle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhysicalVerificationCycleLocation",
                schema: "Verification",
                columns: table => new
                {
                    PhysicalVerificationCycleId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhysicalVerificationCycleLocation", x => new { x.PhysicalVerificationCycleId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_PhysicalVerificationCycleLocation_Cycle_PhysicalVerificationCycleId",
                        column: x => x.PhysicalVerificationCycleId,
                        principalSchema: "Verification",
                        principalTable: "PhysicalVerificationCycle",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerificationCycle_OneActivePerBranch",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                columns: new[] { "BranchId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVerificationAssignment_AuditorUserId",
                schema: "Verification",
                table: "PhysicalVerificationAssignment",
                column: "AuditorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhysicalVerificationCycleLocation_BranchId",
                schema: "Verification",
                table: "PhysicalVerificationCycleLocation",
                column: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhysicalVerificationAssignment",
                schema: "Verification");

            migrationBuilder.DropTable(
                name: "PhysicalVerificationCycleLocation",
                schema: "Verification");

            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerificationCycle_OneActivePerBranch",
                schema: "Verification",
                table: "PhysicalVerificationCycle");

            migrationBuilder.DropColumn(
                name: "BranchId",
                schema: "Verification",
                table: "PhysicalVerificationCycle");

            migrationBuilder.DropColumn(
                name: "TotalAssetCount",
                schema: "Verification",
                table: "PhysicalVerificationCycle");

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerificationCycle_OneActive",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                column: "IsActive",
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
