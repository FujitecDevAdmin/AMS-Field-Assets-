using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.DataImport.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_DataImport_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DataImport");

            migrationBuilder.CreateTable(
                name: "ImportBatch",
                schema: "DataImport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDryRun = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SucceededRows = table.Column<int>(type: "int", nullable: false, defaultValueSql: "0"),
                    FailedRows = table.Column<int>(type: "int", nullable: false),
                    ImportedByUserId = table.Column<int>(type: "int", nullable: false),
                    StartedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatch", x => x.Id);
                    table.CheckConstraint("CK_ImportBatch_Counts", "([TotalRows] >= 0 AND [SucceededRows] >= 0 AND [FailedRows] >= 0 AND [SucceededRows] + [FailedRows] <= [TotalRows])");
                    table.CheckConstraint("CK_ImportBatch_Status", "([Status] IN (N'Running', N'Rehearsed', N'Committed', N'Failed', N'Cancelled'))");
                });

            migrationBuilder.CreateTable(
                name: "ImportError",
                schema: "DataImport",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RawValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    RecordedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportError", x => x.Id);
                    table.CheckConstraint("CK_ImportError_RowNumber", "([RowNumber] > 0)");
                    table.ForeignKey(
                        name: "FK_ImportError_ImportBatch_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalSchema: "DataImport",
                        principalTable: "ImportBatch",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatch_TypeRecent",
                schema: "DataImport",
                table: "ImportBatch",
                columns: new[] { "ImportType", "StartedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ImportBatch_Number",
                schema: "DataImport",
                table: "ImportBatch",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportError_Batch",
                schema: "DataImport",
                table: "ImportError",
                columns: new[] { "ImportBatchId", "RowNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportError",
                schema: "DataImport");

            migrationBuilder.DropTable(
                name: "ImportBatch",
                schema: "DataImport");
        }
    }
}
