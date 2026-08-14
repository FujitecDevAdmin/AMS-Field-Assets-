using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Assets.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Assets_NamedDefaultConstraints : Migration
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
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[Asset]') AND c.name = N'ConcurrencyStamp';
                IF @n IS NULL
                    ALTER TABLE [Assets].[Asset] ADD CONSTRAINT [DF_Asset_ConcurrencyStamp] DEFAULT NEWID() FOR [ConcurrencyStamp];
                ELSE IF @n <> N'DF_Asset_ConcurrencyStamp'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Asset_ConcurrencyStamp', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[Asset]') AND c.name = N'IsBulk';
                IF @n IS NULL
                    ALTER TABLE [Assets].[Asset] ADD CONSTRAINT [DF_Asset_IsBulk] DEFAULT 0 FOR [IsBulk];
                ELSE IF @n <> N'DF_Asset_IsBulk'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Asset_IsBulk', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[Asset]') AND c.name = N'Quantity';
                IF @n IS NULL
                    ALTER TABLE [Assets].[Asset] ADD CONSTRAINT [DF_Asset_Quantity] DEFAULT 1 FOR [Quantity];
                ELSE IF @n <> N'DF_Asset_Quantity'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Asset_Quantity', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'IsAllocatable';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_IsAllocatable] DEFAULT 1 FOR [IsAllocatable];
                ELSE IF @n <> N'DF_AssetType_IsAllocatable'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_IsAllocatable', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'IsBulkDefault';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_IsBulkDefault] DEFAULT 0 FOR [IsBulkDefault];
                ELSE IF @n <> N'DF_AssetType_IsBulkDefault'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_IsBulkDefault', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'IsPhysical';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_IsPhysical] DEFAULT 1 FOR [IsPhysical];
                ELSE IF @n <> N'DF_AssetType_IsPhysical'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_IsPhysical', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'TracksCalibration';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_TracksCalibration] DEFAULT 0 FOR [TracksCalibration];
                ELSE IF @n <> N'DF_AssetType_TracksCalibration'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_TracksCalibration', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'TracksHardware';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_TracksHardware] DEFAULT 0 FOR [TracksHardware];
                ELSE IF @n <> N'DF_AssetType_TracksHardware'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_TracksHardware', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'TracksSoftware';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_TracksSoftware] DEFAULT 0 FOR [TracksSoftware];
                ELSE IF @n <> N'DF_AssetType_TracksSoftware'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_TracksSoftware', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Assets].[AssetType]') AND c.name = N'TracksVehicle';
                IF @n IS NULL
                    ALTER TABLE [Assets].[AssetType] ADD CONSTRAINT [DF_AssetType_TracksVehicle] DEFAULT 0 FOR [TracksVehicle];
                ELSE IF @n <> N'DF_AssetType_TracksVehicle'
                BEGIN
                    SET @q = N'[Assets].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_AssetType_TracksVehicle', @objtype = N'OBJECT';
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
