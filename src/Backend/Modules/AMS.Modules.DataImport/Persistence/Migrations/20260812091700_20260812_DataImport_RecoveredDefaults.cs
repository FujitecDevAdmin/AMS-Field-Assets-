using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.DataImport.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_DataImport_RecoveredDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TotalRows",
                schema: "DataImport",
                table: "ImportBatch",
                type: "int",
                nullable: false,
                defaultValueSql: "0",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "FailedRows",
                schema: "DataImport",
                table: "ImportBatch",
                type: "int",
                nullable: false,
                defaultValueSql: "0",
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TotalRows",
                schema: "DataImport",
                table: "ImportBatch",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "0");

            migrationBuilder.AlterColumn<int>(
                name: "FailedRows",
                schema: "DataImport",
                table: "ImportBatch",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "0");
        }
    }
}
