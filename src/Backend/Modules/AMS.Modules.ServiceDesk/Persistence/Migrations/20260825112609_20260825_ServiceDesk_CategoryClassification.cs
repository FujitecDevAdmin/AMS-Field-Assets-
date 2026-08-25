using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceDesk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260825_ServiceDesk_CategoryClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryType",
                schema: "ServiceDesk",
                table: "RequestCategory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestSubCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [ServiceDesk].[RequestCategory] AS c
                    WHERE (EXISTS (
                        SELECT 1 FROM [ServiceDesk].[ServiceRequest] AS r
                        WHERE r.[RequestCategoryId] = c.[Id] AND r.[RequestKind] = N'NewService')
                        OR EXISTS (
                        SELECT 1 FROM [ServiceDesk].[ServiceTemplate] AS t
                        WHERE t.[RequestCategoryId] = c.[Id] AND t.[RequestKind] = N'NewService'))
                      AND (EXISTS (
                        SELECT 1 FROM [ServiceDesk].[ServiceRequest] AS r
                        WHERE r.[RequestCategoryId] = c.[Id]
                          AND r.[RequestKind] IN (N'SupportTicket', N'AssetIssue'))
                        OR EXISTS (
                        SELECT 1 FROM [ServiceDesk].[ServiceTemplate] AS t
                        WHERE t.[RequestCategoryId] = c.[Id]
                          AND t.[RequestKind] IN (N'SupportTicket', N'AssetIssue'))))
                    THROW 51000, 'A request category is used by both Service and Incident requests. Split and remap it before applying this migration.', 1;

                UPDATE c
                SET [CategoryType] = CASE WHEN EXISTS (
                    SELECT 1 FROM [ServiceDesk].[ServiceRequest] AS r
                    WHERE r.[RequestCategoryId] = c.[Id] AND r.[RequestKind] = N'NewService')
                    OR EXISTS (
                    SELECT 1 FROM [ServiceDesk].[ServiceTemplate] AS t
                    WHERE t.[RequestCategoryId] = c.[Id] AND t.[RequestKind] = N'NewService')
                    THEN N'Service' ELSE N'Incident' END
                FROM [ServiceDesk].[RequestCategory] AS c;

                UPDATE d
                SET [RequestCategoryId] = r.[RequestCategoryId],
                    [RequestSubCategoryId] = r.[RequestSubCategoryId]
                FROM [ServiceDesk].[NewServiceRequestDetail] AS d
                INNER JOIN [ServiceDesk].[ServiceRequest] AS r
                    ON r.[Id] = d.[ServiceRequestId];

                IF EXISTS (
                    SELECT 1 FROM [ServiceDesk].[NewServiceRequestDetail]
                    WHERE [RequestCategoryId] IS NULL OR [RequestSubCategoryId] IS NULL)
                    THROW 51001, 'An existing New Service request is unclassified. Classify it before applying this migration.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [ServiceDesk].[NewServiceRequestDetail] AS d
                    INNER JOIN [ServiceDesk].[RequestSubCategory] AS s
                        ON s.[Id] = d.[RequestSubCategoryId]
                    WHERE s.[RequestCategoryId] <> d.[RequestCategoryId])
                    THROW 51002, 'An existing New Service request has a category/sub-category mismatch.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CategoryType",
                schema: "ServiceDesk",
                table: "RequestCategory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestSubCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RequestSubCategory_Id_RequestCategoryId",
                schema: "ServiceDesk",
                table: "RequestSubCategory",
                columns: new[] { "Id", "RequestCategoryId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RequestCategory_CategoryType",
                schema: "ServiceDesk",
                table: "RequestCategory",
                sql: "([CategoryType] IN (N'Service', N'Incident'))");

            migrationBuilder.AddForeignKey(
                name: "FK_NewServiceRequestDetail_RequestCategory_RequestCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                column: "RequestCategoryId",
                principalSchema: "ServiceDesk",
                principalTable: "RequestCategory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewServiceRequestDetail_RequestSubCategory_Category",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                columns: new[] { "RequestSubCategoryId", "RequestCategoryId" },
                principalSchema: "ServiceDesk",
                principalTable: "RequestSubCategory",
                principalColumns: new[] { "Id", "RequestCategoryId" });

            migrationBuilder.DropColumn(name: "NeedsDms", schema: "ServiceDesk", table: "NewServiceRequestDetail");
            migrationBuilder.DropColumn(name: "NeedsEmail", schema: "ServiceDesk", table: "NewServiceRequestDetail");
            migrationBuilder.DropColumn(name: "NeedsErp", schema: "ServiceDesk", table: "NewServiceRequestDetail");
            migrationBuilder.DropColumn(name: "NeedsVpn", schema: "ServiceDesk", table: "NewServiceRequestDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewServiceRequestDetail_RequestCategory_RequestCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_NewServiceRequestDetail_RequestSubCategory_Category",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RequestSubCategory_Id_RequestCategoryId",
                schema: "ServiceDesk",
                table: "RequestSubCategory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RequestCategory_CategoryType",
                schema: "ServiceDesk",
                table: "RequestCategory");

            migrationBuilder.DropColumn(
                name: "CategoryType",
                schema: "ServiceDesk",
                table: "RequestCategory");

            migrationBuilder.DropColumn(
                name: "RequestCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail");

            migrationBuilder.DropColumn(
                name: "RequestSubCategoryId",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsDms",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsEmail",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsErp",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsVpn",
                schema: "ServiceDesk",
                table: "NewServiceRequestDetail",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
