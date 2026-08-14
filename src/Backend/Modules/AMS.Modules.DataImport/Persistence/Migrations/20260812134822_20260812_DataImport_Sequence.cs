using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.DataImport.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_DataImport_Sequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ImportBatchNumberSequence",
                schema: "DataImport");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "ImportBatchNumberSequence",
                schema: "DataImport");
        }
    }
}
