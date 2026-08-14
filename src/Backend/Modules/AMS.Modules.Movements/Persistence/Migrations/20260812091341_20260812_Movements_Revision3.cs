using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Movements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Movements_Revision3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                schema: "Movements",
                table: "AssetMovement",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValueSql: "1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetMovement_QuantityPositive",
                schema: "Movements",
                table: "AssetMovement",
                sql: "([Quantity] > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetMovement_QuantityPositive",
                schema: "Movements",
                table: "AssetMovement");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "Movements",
                table: "AssetMovement");
        }
    }
}
