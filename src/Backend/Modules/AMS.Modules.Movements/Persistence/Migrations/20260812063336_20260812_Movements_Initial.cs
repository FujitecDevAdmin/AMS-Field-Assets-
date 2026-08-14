using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Movements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Movements_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Movements");

            migrationBuilder.CreateTable(
                name: "MovementBatch",
                schema: "Movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CourierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ChallanNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DocumentPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    DispatchedByUserId = table.Column<int>(type: "int", nullable: false),
                    ShippedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovementBatch", x => x.Id);
                    table.CheckConstraint("CK_MovementBatch_DifferentBranches", "([FromLocationId] <> [ToLocationId])");
                    table.CheckConstraint("CK_MovementBatch_PositiveCount", "([ItemCount] > 0)");
                    table.CheckConstraint("CK_MovementBatch_Type", "([MovementType] IN (N'Transfer', N'HandoverToHO'))");
                });

            migrationBuilder.CreateTable(
                name: "AssetMovement",
                schema: "Movements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    MovementBatchId = table.Column<int>(type: "int", nullable: true),
                    HandoverId = table.Column<int>(type: "int", nullable: true),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CourierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ChallanNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ShippedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceiptRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMovement", x => x.Id);
                    table.CheckConstraint("CK_AssetMovement_DifferentBranches", "([FromLocationId] <> [ToLocationId])");
                    table.CheckConstraint("CK_AssetMovement_ReceiptPair", "(([Status] = N'Received' AND [ReceivedOnUtc] IS NOT NULL) OR ([Status] <> N'Received' AND [ReceivedOnUtc] IS NULL))");
                    table.CheckConstraint("CK_AssetMovement_Status", "([Status] IN (N'InTransit', N'Received', N'Cancelled'))");
                    table.CheckConstraint("CK_AssetMovement_Type", "([MovementType] IN (N'Transfer', N'HandoverToHO'))");
                    table.ForeignKey(
                        name: "FK_AssetMovement_MovementBatch_MovementBatchId",
                        column: x => x.MovementBatchId,
                        principalSchema: "Movements",
                        principalTable: "MovementBatch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovement_Batch",
                schema: "Movements",
                table: "AssetMovement",
                column: "MovementBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovement_Handover",
                schema: "Movements",
                table: "AssetMovement",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMovement_Incoming",
                schema: "Movements",
                table: "AssetMovement",
                columns: new[] { "Status", "ToLocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovementBatch_Open",
                schema: "Movements",
                table: "MovementBatch",
                columns: new[] { "ToLocationId", "ShippedOnUtc" },
                filter: "[ReceivedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_MovementBatch_Number",
                schema: "Movements",
                table: "MovementBatch",
                column: "BatchNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMovement",
                schema: "Movements");

            migrationBuilder.DropTable(
                name: "MovementBatch",
                schema: "Movements");
        }
    }
}
