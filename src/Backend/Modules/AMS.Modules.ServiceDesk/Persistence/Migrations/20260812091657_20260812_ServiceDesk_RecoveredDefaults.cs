using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceDesk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceDesk_RecoveredDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssetCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestItem",
                newName: "AssetTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssetTypeId",
                schema: "ServiceDesk",
                table: "NewServiceRequestItem",
                newName: "AssetCategoryId");
        }
    }
}
