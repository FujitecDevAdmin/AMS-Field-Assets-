using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Organization.Persistence.Migrations;

/// <inheritdoc />
public partial class _20260817_Organization_RenameLocationToBranch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "Location",
            schema: "Organization",
            newName: "Branch",
            newSchema: "Organization");

        migrationBuilder.RenameColumn(
            name: "LocationCode",
            schema: "Organization",
            table: "Branch",
            newName: "BranchCode");

        migrationBuilder.RenameColumn(
            name: "LocationName",
            schema: "Organization",
            table: "Branch",
            newName: "BranchName");

        migrationBuilder.RenameColumn(
            name: "LocationId",
            schema: "Organization",
            table: "Employee",
            newName: "BranchId");

        migrationBuilder.RenameIndex(
            name: "IX_Location_RegionId",
            schema: "Organization",
            table: "Branch",
            newName: "IX_Branch_RegionId");

        migrationBuilder.RenameIndex(
            name: "UX_Location_Code",
            schema: "Organization",
            table: "Branch",
            newName: "UX_Branch_Code");

        migrationBuilder.RenameIndex(
            name: "UX_Location_OneHeadOffice",
            schema: "Organization",
            table: "Branch",
            newName: "UX_Branch_OneHeadOffice");

        migrationBuilder.RenameIndex(
            name: "IX_Employee_Location",
            schema: "Organization",
            table: "Employee",
            newName: "IX_Employee_Branch");

        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[PK_Location]', N'PK_Branch', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[FK_Location_Region_RegionId]', N'FK_Branch_Region_RegionId', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[DF_Location_TimeZoneId]', N'DF_Branch_TimeZoneId', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[FK_Employee_Location_LocationId]', N'FK_Employee_Branch_BranchId', N'OBJECT';");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[FK_Employee_Branch_BranchId]', N'FK_Employee_Location_LocationId', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[DF_Branch_TimeZoneId]', N'DF_Location_TimeZoneId', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[FK_Branch_Region_RegionId]', N'FK_Location_Region_RegionId', N'OBJECT';");
        migrationBuilder.Sql("EXEC sp_rename N'[Organization].[PK_Branch]', N'PK_Location', N'OBJECT';");

        migrationBuilder.RenameIndex(
            name: "IX_Employee_Branch",
            schema: "Organization",
            table: "Employee",
            newName: "IX_Employee_Location");

        migrationBuilder.RenameIndex(
            name: "UX_Branch_OneHeadOffice",
            schema: "Organization",
            table: "Branch",
            newName: "UX_Location_OneHeadOffice");

        migrationBuilder.RenameIndex(
            name: "UX_Branch_Code",
            schema: "Organization",
            table: "Branch",
            newName: "UX_Location_Code");

        migrationBuilder.RenameIndex(
            name: "IX_Branch_RegionId",
            schema: "Organization",
            table: "Branch",
            newName: "IX_Location_RegionId");

        migrationBuilder.RenameColumn(
            name: "BranchId",
            schema: "Organization",
            table: "Employee",
            newName: "LocationId");

        migrationBuilder.RenameColumn(
            name: "BranchName",
            schema: "Organization",
            table: "Branch",
            newName: "LocationName");

        migrationBuilder.RenameColumn(
            name: "BranchCode",
            schema: "Organization",
            table: "Branch",
            newName: "LocationCode");

        migrationBuilder.RenameTable(
            name: "Branch",
            schema: "Organization",
            newName: "Location",
            newSchema: "Organization");
    }
}
