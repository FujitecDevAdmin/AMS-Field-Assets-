using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Allocations.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Allocations_Revision3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "Allocations",
                table: "CustomerSite",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CommissionedDate",
                schema: "Allocations",
                table: "AssetSiteMapping",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "Allocations",
                table: "CustomerSite");

            migrationBuilder.DropColumn(
                name: "CommissionedDate",
                schema: "Allocations",
                table: "AssetSiteMapping");
        }
    }
}
