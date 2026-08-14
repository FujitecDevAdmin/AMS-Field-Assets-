using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.DataImport.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_DataImport_NamedDefaultConstraints : Migration
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
                WHERE  dc.parent_object_id = OBJECT_ID(N'[DataImport].[ImportBatch]') AND c.name = N'FailedRows';
                IF @n IS NULL
                    ALTER TABLE [DataImport].[ImportBatch] ADD CONSTRAINT [DF_ImportBatch_FailedRows] DEFAULT 0 FOR [FailedRows];
                ELSE IF @n <> N'DF_ImportBatch_FailedRows'
                BEGIN
                    SET @q = N'[DataImport].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ImportBatch_FailedRows', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[DataImport].[ImportBatch]') AND c.name = N'SucceededRows';
                IF @n IS NULL
                    ALTER TABLE [DataImport].[ImportBatch] ADD CONSTRAINT [DF_ImportBatch_SucceededRows] DEFAULT 0 FOR [SucceededRows];
                ELSE IF @n <> N'DF_ImportBatch_SucceededRows'
                BEGIN
                    SET @q = N'[DataImport].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ImportBatch_SucceededRows', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[DataImport].[ImportBatch]') AND c.name = N'TotalRows';
                IF @n IS NULL
                    ALTER TABLE [DataImport].[ImportBatch] ADD CONSTRAINT [DF_ImportBatch_TotalRows] DEFAULT 0 FOR [TotalRows];
                ELSE IF @n <> N'DF_ImportBatch_TotalRows'
                BEGIN
                    SET @q = N'[DataImport].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ImportBatch_TotalRows', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[DataImport].[ImportError]') AND c.name = N'IsResolved';
                IF @n IS NULL
                    ALTER TABLE [DataImport].[ImportError] ADD CONSTRAINT [DF_ImportError_IsResolved] DEFAULT 0 FOR [IsResolved];
                ELSE IF @n <> N'DF_ImportError_IsResolved'
                BEGIN
                    SET @q = N'[DataImport].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ImportError_IsResolved', @objtype = N'OBJECT';
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
