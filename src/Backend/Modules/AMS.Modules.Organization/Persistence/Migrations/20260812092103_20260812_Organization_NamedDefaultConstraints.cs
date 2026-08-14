using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Organization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Organization_NamedDefaultConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HAND-REPLACED by build/rewrite_default_migrations.py.
            //
            // The scaffolder emitted AlterColumn for each of these, because to
            // EF a renamed DEFAULT constraint is a changed column. SQL Server
            // rewrites the column for an AlterColumn and refuses outright when
            // an index depends on it, so this migration failed with error 5074
            // on IX_AssetHandover_GrnQueue and eight other filtered indexes.
            //
            // Naming a constraint needs no column change. sp_rename does it in
            // place. The old name is one SQL Server invented - it differs on
            // every database - so each block finds the constraint by COLUMN,
            // and skips silently if it already carries the right name.
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Organization].[Employee]') AND c.name = N'ConcurrencyStamp';
                IF @n IS NULL
                    ALTER TABLE [Organization].[Employee] ADD CONSTRAINT [DF_Employee_ConcurrencyStamp] DEFAULT NEWID() FOR [ConcurrencyStamp];
                ELSE IF @n <> N'DF_Employee_ConcurrencyStamp'
                BEGIN
                    SET @q = N'[Organization].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Employee_ConcurrencyStamp', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Organization].[Location]') AND c.name = N'TimeZoneId';
                IF @n IS NULL
                    ALTER TABLE [Organization].[Location] ADD CONSTRAINT [DF_Location_TimeZoneId] DEFAULT N'India Standard Time' FOR [TimeZoneId];
                ELSE IF @n <> N'DF_Location_TimeZoneId'
                BEGIN
                    SET @q = N'[Organization].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Location_TimeZoneId', @objtype = N'OBJECT';
                END
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty. Down() would have to restore names like
            // DF__AssetType__IsAll__395884C4, which SQL Server generated and
            // which differ per database, so there is nothing to restore TO.
            // Reverting this migration leaves the defaults correctly named,
            // which is harmless: the next Up() finds them already right.

        }
    }
}
