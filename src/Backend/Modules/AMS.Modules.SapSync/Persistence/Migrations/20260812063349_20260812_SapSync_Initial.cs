using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.SapSync.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_SapSync_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SapSync");

            migrationBuilder.CreateTable(
                name: "SapSyncLog",
                schema: "SapSync",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SapSyncLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SapSyncWatermark",
                schema: "SapSync",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastChangedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SapSyncWatermark", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SapSyncLog_Failures",
                schema: "SapSync",
                table: "SapSyncLog",
                columns: new[] { "Outcome", "StartedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SapSyncLog_Recent",
                schema: "SapSync",
                table: "SapSyncLog",
                column: "StartedOnUtc");

            migrationBuilder.CreateIndex(
                name: "UX_SapSyncWatermark_Type",
                schema: "SapSync",
                table: "SapSyncWatermark",
                column: "SyncType",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SapSyncLog",
                schema: "SapSync");

            migrationBuilder.DropTable(
                name: "SapSyncWatermark",
                schema: "SapSync");
        }
    }
}
