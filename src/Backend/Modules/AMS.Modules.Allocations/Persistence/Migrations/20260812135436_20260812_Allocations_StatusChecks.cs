using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Allocations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Allocations_StatusChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetAllocationApproval_Status",
                schema: "Allocations",
                table: "AssetAllocationApproval",
                sql: "([Status] IN (N'Pending', N'BranchApproved', N'Approved', N'Rejected', N'Cancelled'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetAcknowledgement_Status",
                schema: "Allocations",
                table: "AssetAcknowledgement",
                sql: "([Status] IN (N'Pending', N'Signed', N'Approved'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetAllocationApproval_Status",
                schema: "Allocations",
                table: "AssetAllocationApproval");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetAcknowledgement_Status",
                schema: "Allocations",
                table: "AssetAcknowledgement");
        }
    }
}
