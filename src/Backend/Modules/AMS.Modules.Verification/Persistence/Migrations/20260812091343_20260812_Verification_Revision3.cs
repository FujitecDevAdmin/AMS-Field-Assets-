using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Verification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Verification_Revision3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerification_OnePerAssetPerCycle",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.AddColumn<decimal>(
                name: "CountedQuantity",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedQuantitySnapshot",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBulkCount",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "bit",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerification_OneBulkCountPerPlacePerCycle",
                schema: "Verification",
                table: "PhysicalVerification",
                columns: new[] { "PhysicalVerificationCycleId", "AssetId", "LocationId" },
                unique: true,
                filter: "[IsBulkCount] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerification_OnePerUnitAssetPerCycle",
                schema: "Verification",
                table: "PhysicalVerification",
                columns: new[] { "PhysicalVerificationCycleId", "AssetId" },
                unique: true,
                filter: "[IsBulkCount] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_BulkHasCount",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([IsBulkCount] = 0 OR [CountedQuantity] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_CountNonNegative",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([CountedQuantity] IS NULL OR [CountedQuantity] >= 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerification_OneBulkCountPerPlacePerCycle",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerification_OnePerUnitAssetPerCycle",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_BulkHasCount",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_CountNonNegative",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "CountedQuantity",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "ExpectedQuantitySnapshot",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "IsBulkCount",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerification_OnePerAssetPerCycle",
                schema: "Verification",
                table: "PhysicalVerification",
                columns: new[] { "PhysicalVerificationCycleId", "AssetId" },
                unique: true);
        }
    }
}
