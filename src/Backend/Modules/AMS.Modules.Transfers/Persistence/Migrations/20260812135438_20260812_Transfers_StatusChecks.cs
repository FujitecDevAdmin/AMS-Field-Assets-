using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Transfers.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Transfers_StatusChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetTransferRequest_SapSyncStatus",
                schema: "Transfers",
                table: "AssetTransferRequest",
                sql: "([SapSyncStatus] IN (N'NotRequired', N'Pending', N'Sent', N'Failed'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetTransferRequest_Status",
                schema: "Transfers",
                table: "AssetTransferRequest",
                sql: "([Status] IN (N'Pending', N'Approved', N'Rejected', N'Completed', N'Cancelled'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetTransferRequest_SapSyncStatus",
                schema: "Transfers",
                table: "AssetTransferRequest");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetTransferRequest_Status",
                schema: "Transfers",
                table: "AssetTransferRequest");
        }
    }
}
