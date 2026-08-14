using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceLevel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceLevel_RecoveredDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RespectWeekends",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "bit",
                nullable: false,
                defaultValueSql: "1",
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "RespectHolidays",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "bit",
                nullable: false,
                defaultValueSql: "1",
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "NearDueWarningMinutes",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "int",
                nullable: false,
                defaultValueSql: "30",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "DeferNewTicketsOnFriday",
                schema: "ServiceLevel",
                table: "LocationOperationalHour",
                type: "bit",
                nullable: false,
                defaultValueSql: "0",
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "RespectWeekends",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "1");

            migrationBuilder.AlterColumn<bool>(
                name: "RespectHolidays",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "1");

            migrationBuilder.AlterColumn<int>(
                name: "NearDueWarningMinutes",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "30");

            migrationBuilder.AlterColumn<bool>(
                name: "DeferNewTicketsOnFriday",
                schema: "ServiceLevel",
                table: "LocationOperationalHour",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValueSql: "0");
        }
    }
}
