using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceDesk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceDesk_Sequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "RequestNumberSequence",
                schema: "ServiceDesk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "RequestNumberSequence",
                schema: "ServiceDesk");
        }
    }
}
