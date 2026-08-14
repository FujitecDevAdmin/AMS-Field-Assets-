using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Allocations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Allocations_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Allocations");

            migrationBuilder.CreateTable(
                name: "AssetAllocation",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    AllocatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturnRequestedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAllocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetAllocationApproval",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AllocationId = table.Column<int>(type: "int", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAllocationApproval", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSite",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetAcknowledgement",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DocumentPath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SignatureImagePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SignedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerUserId = table.Column<int>(type: "int", nullable: true),
                    ManagerApprovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAcknowledgement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetAcknowledgement_AssetAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "Allocations",
                        principalTable: "AssetAllocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetHandover",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    FromEmployeeId = table.Column<int>(type: "int", nullable: false),
                    BranchLocationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReturnCondition = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HandedOverOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: false),
                    MovementId = table.Column<int>(type: "int", nullable: true),
                    DispatchedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsReceivedByHo = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    ReceivedAtHoOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAtHoByUserId = table.Column<int>(type: "int", nullable: true),
                    ReceiptRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHandover", x => x.Id);
                    table.CheckConstraint("CK_AssetHandover_CancelPair", "(([Status] = N'Cancelled' AND [CancelledOnUtc] IS NOT NULL) OR ([Status] <> N'Cancelled' AND [CancelledOnUtc] IS NULL))");
                    table.CheckConstraint("CK_AssetHandover_Condition", "([ReturnCondition] IN (N'Good', N'MinorDamage', N'Damaged', N'NotWorking', N'Missing'))");
                    table.CheckConstraint("CK_AssetHandover_ReceiptPair", "(([IsReceivedByHo] = 0 AND [ReceivedAtHoOnUtc] IS NULL) OR ([IsReceivedByHo] = 1 AND [ReceivedAtHoOnUtc] IS NOT NULL))");
                    table.CheckConstraint("CK_AssetHandover_ReceiptStatus", "(([Status] = N'ReceivedAtHo' AND [IsReceivedByHo] = 1) OR ([Status] <> N'ReceivedAtHo' AND [IsReceivedByHo] = 0))");
                    table.CheckConstraint("CK_AssetHandover_Status", "([Status] IN (N'HandedOver', N'InTransitToHo', N'ReceivedAtHo', N'Cancelled'))");
                    table.ForeignKey(
                        name: "FK_AssetHandover_AssetAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "Allocations",
                        principalTable: "AssetAllocation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetSiteMapping",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    CustomerSiteId = table.Column<int>(type: "int", nullable: false),
                    MappedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSiteMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetSiteMapping_CustomerSite_CustomerSiteId",
                        column: x => x.CustomerSiteId,
                        principalSchema: "Allocations",
                        principalTable: "CustomerSite",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AllocationReturnReversal",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<int>(type: "int", nullable: false),
                    HandoverId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousReturnedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousAssetStatusId = table.Column<int>(type: "int", nullable: true),
                    RestoredEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ReversedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReversedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllocationReturnReversal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllocationReturnReversal_AssetAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "Allocations",
                        principalTable: "AssetAllocation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AllocationReturnReversal_AssetHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "Allocations",
                        principalTable: "AssetHandover",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetReturnImage",
                schema: "Allocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationId = table.Column<int>(type: "int", nullable: false),
                    HandoverId = table.Column<int>(type: "int", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    CapturedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetReturnImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetReturnImage_AssetAllocation_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "Allocations",
                        principalTable: "AssetAllocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetReturnImage_AssetHandover_HandoverId",
                        column: x => x.HandoverId,
                        principalSchema: "Allocations",
                        principalTable: "AssetHandover",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllocationReturnReversal_AllocationId",
                schema: "Allocations",
                table: "AllocationReturnReversal",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetAcknowledgement_Allocation",
                schema: "Allocations",
                table: "AssetAcknowledgement",
                column: "AllocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetAllocation_LocationEmployee",
                schema: "Allocations",
                table: "AssetAllocation",
                columns: new[] { "LocationId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAllocation_Overdue",
                schema: "Allocations",
                table: "AssetAllocation",
                column: "ExpectedReturnDate",
                filter: "[ReturnedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AssetAllocation_OneActivePerAsset",
                schema: "Allocations",
                table: "AssetAllocation",
                column: "AssetId",
                unique: true,
                filter: "[ReturnedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAllocationApproval_Queue",
                schema: "Allocations",
                table: "AssetAllocationApproval",
                columns: new[] { "Status", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHandover_BranchQueue",
                schema: "Allocations",
                table: "AssetHandover",
                columns: new[] { "BranchLocationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHandover_GrnQueue",
                schema: "Allocations",
                table: "AssetHandover",
                columns: new[] { "Status", "DispatchedOnUtc" },
                filter: "[IsReceivedByHo] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AssetHandover_OneOpenPerAsset",
                schema: "Allocations",
                table: "AssetHandover",
                column: "AssetId",
                unique: true,
                filter: "[Status] = N'HandedOver'");

            migrationBuilder.CreateIndex(
                name: "UX_AssetHandover_OnePerAllocation",
                schema: "Allocations",
                table: "AssetHandover",
                column: "AllocationId",
                unique: true,
                filter: "[CancelledOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReturnImage_AllocationId",
                schema: "Allocations",
                table: "AssetReturnImage",
                column: "AllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReturnImage_HandoverId",
                schema: "Allocations",
                table: "AssetReturnImage",
                column: "HandoverId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetSiteMapping_CustomerSiteId",
                schema: "Allocations",
                table: "AssetSiteMapping",
                column: "CustomerSiteId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetSiteMapping_OneActivePerAsset",
                schema: "Allocations",
                table: "AssetSiteMapping",
                column: "AssetId",
                unique: true,
                filter: "[RemovedOnUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllocationReturnReversal",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetAcknowledgement",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetAllocationApproval",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetReturnImage",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetSiteMapping",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetHandover",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "CustomerSite",
                schema: "Allocations");

            migrationBuilder.DropTable(
                name: "AssetAllocation",
                schema: "Allocations");
        }
    }
}
