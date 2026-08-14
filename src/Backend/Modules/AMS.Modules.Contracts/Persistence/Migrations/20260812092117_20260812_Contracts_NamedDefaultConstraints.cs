using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Contracts.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_Contracts_NamedDefaultConstraints : Migration
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
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Contracts].[Contract]') AND c.name = N'ConcurrencyStamp';
                IF @n IS NULL
                    ALTER TABLE [Contracts].[Contract] ADD CONSTRAINT [DF_Contract_ConcurrencyStamp] DEFAULT NEWID() FOR [ConcurrencyStamp];
                ELSE IF @n <> N'DF_Contract_ConcurrencyStamp'
                BEGIN
                    SET @q = N'[Contracts].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_Contract_ConcurrencyStamp', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Contracts].[ContractReminderLog]') AND c.name = N'Outcome';
                IF @n IS NULL
                    ALTER TABLE [Contracts].[ContractReminderLog] ADD CONSTRAINT [DF_ContractReminderLog_Outcome] DEFAULT N'Queued' FOR [Outcome];
                ELSE IF @n <> N'DF_ContractReminderLog_Outcome'
                BEGIN
                    SET @q = N'[Contracts].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ContractReminderLog_Outcome', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[Contracts].[ContractReminderSetting]') AND c.name = N'Channel';
                IF @n IS NULL
                    ALTER TABLE [Contracts].[ContractReminderSetting] ADD CONSTRAINT [DF_ContractReminderSetting_Channel] DEFAULT N'Email' FOR [Channel];
                ELSE IF @n <> N'DF_ContractReminderSetting_Channel'
                BEGIN
                    SET @q = N'[Contracts].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ContractReminderSetting_Channel', @objtype = N'OBJECT';
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
