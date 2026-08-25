using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Organization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260824_Organization_AddBranchCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "Organization",
                table: "Branch",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "Organization",
                table: "Branch",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Branch_Latitude",
                schema: "Organization",
                table: "Branch",
                sql: "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Branch_Longitude",
                schema: "Organization",
                table: "Branch",
                sql: "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Branch_Latitude",
                schema: "Organization",
                table: "Branch");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Branch_Longitude",
                schema: "Organization",
                table: "Branch");

            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "Organization",
                table: "Branch");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "Organization",
                table: "Branch");
        }
    }
}
