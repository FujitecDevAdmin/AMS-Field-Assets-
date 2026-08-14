using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Movements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Movements_Sequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MovementBatchNumberSequence",
                schema: "Movements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "MovementBatchNumberSequence",
                schema: "Movements");
        }
    }
}
