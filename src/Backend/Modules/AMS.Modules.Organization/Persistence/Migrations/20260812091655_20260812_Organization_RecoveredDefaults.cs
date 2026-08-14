using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Organization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Organization_RecoveredDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                schema: "Organization",
                table: "Location",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValueSql: "N'India Standard Time'",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                schema: "Organization",
                table: "Location",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldDefaultValueSql: "N'India Standard Time'");
        }
    }
}
