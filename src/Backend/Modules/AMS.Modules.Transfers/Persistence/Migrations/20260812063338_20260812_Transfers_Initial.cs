using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Transfers.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Transfers_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Transfers");

            migrationBuilder.CreateTable(
                name: "AssetTransferRequest",
                schema: "Transfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    TransferType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ToEmployeeId = table.Column<int>(type: "int", nullable: true),
                    FromDepartmentId = table.Column<int>(type: "int", nullable: true),
                    ToDepartmentId = table.Column<int>(type: "int", nullable: true),
                    FromLocationId = table.Column<int>(type: "int", nullable: true),
                    ToLocationId = table.Column<int>(type: "int", nullable: true),
                    FromCostCenter = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ToCostCenter = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MovementId = table.Column<int>(type: "int", nullable: true),
                    SapSyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransferRequest", x => x.Id);
                    table.CheckConstraint("CK_AssetTransferRequest_TypePair", "(([TransferType] = 'Employee' AND [ToEmployeeId] IS NOT NULL) OR ([TransferType] = 'Department' AND [ToDepartmentId] IS NOT NULL) OR ([TransferType] = 'Branch' AND [ToLocationId] IS NOT NULL) OR ([TransferType] = 'CostCenter' AND [ToCostCenter] IS NOT NULL))");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransferRequest_Queue",
                schema: "Transfers",
                table: "AssetTransferRequest",
                columns: new[] { "Status", "FromLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransferRequest_SapPending",
                schema: "Transfers",
                table: "AssetTransferRequest",
                column: "SapSyncStatus",
                filter: "[SapSyncStatus] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetTransferRequest",
                schema: "Transfers");
        }
    }
}
