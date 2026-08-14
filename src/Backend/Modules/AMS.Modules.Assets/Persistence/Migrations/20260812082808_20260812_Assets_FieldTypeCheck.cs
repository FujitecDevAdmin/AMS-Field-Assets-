using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Assets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Assets_FieldTypeCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition",
                sql: "[FieldType] IN (N'Text', N'Number', N'Percentage', N'Date', N'Boolean', N'Dropdown')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomFieldDefinition_Type",
                schema: "Assets",
                table: "CustomFieldDefinition");
        }
    }
}
