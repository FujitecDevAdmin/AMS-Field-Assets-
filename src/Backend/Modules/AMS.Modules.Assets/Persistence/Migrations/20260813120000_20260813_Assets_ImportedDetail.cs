using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AMS.Modules.Assets.Persistence;

#nullable disable

namespace AMS.Modules.Assets.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(AssetsDbContext))]
[Migration("20260813120000_20260813_Assets_ImportedDetail")]
public partial class _20260813_Assets_ImportedDetail : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImportedDataJson",
            schema: "Assets",
            table: "Asset",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ImportedDataJson",
            schema: "Assets",
            table: "Asset");
    }
}
