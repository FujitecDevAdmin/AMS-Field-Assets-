using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260817_Identity_RenameUserBranchLocationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationId",
                schema: "Identity",
                table: "UserBranch",
                newName: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BranchId",
                schema: "Identity",
                table: "UserBranch",
                newName: "LocationId");
        }
    }
}
