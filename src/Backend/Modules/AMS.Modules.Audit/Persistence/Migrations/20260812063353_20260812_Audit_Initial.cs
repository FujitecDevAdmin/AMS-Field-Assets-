using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Audit.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Audit_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Audit");

            migrationBuilder.CreateTable(
                name: "AssetFieldAudit",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFieldAudit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledFieldChange",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchemaName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CurrentValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    EffectiveFromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledFieldChange", x => x.Id);
                    table.CheckConstraint("CK_ScheduledFieldChange_Applied", "([Status] <> N'Applied' OR [AppliedOnUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_ScheduledFieldChange_Status", "([Status] IN (N'Pending', N'Applied', N'Cancelled', N'Failed', N'Superseded'))");
                    table.CheckConstraint("CK_ScheduledFieldChange_Window", "([EffectiveToDate] IS NULL OR [EffectiveToDate] >= [EffectiveFromDate])");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AFA_Asset",
                schema: "Audit",
                table: "AssetFieldAudit",
                columns: new[] { "AssetId", "ChangedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledFieldChange_Due",
                schema: "Audit",
                table: "ScheduledFieldChange",
                column: "EffectiveFromDate",
                filter: "[Status] = N'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledFieldChange_Entity",
                schema: "Audit",
                table: "ScheduledFieldChange",
                columns: new[] { "SchemaName", "EntityName", "EntityId", "EffectiveFromDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ScheduledFieldChange_OnePendingPerFieldPerDate",
                schema: "Audit",
                table: "ScheduledFieldChange",
                columns: new[] { "SchemaName", "EntityName", "EntityId", "FieldName", "EffectiveFromDate" },
                unique: true,
                filter: "[Status] = N'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetFieldAudit",
                schema: "Audit");

            migrationBuilder.DropTable(
                name: "ScheduledFieldChange",
                schema: "Audit");
        }
    }
}
