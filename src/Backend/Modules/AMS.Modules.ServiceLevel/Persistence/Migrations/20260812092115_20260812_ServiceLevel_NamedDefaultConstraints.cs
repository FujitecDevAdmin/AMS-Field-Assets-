using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceLevel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceLevel_NamedDefaultConstraints : Migration
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
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[HolidayCalendar]') AND c.name = N'IsRecurringAnnually';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[HolidayCalendar] ADD CONSTRAINT [DF_HolidayCalendar_IsRecurring] DEFAULT 0 FOR [IsRecurringAnnually];
                ELSE IF @n <> N'DF_HolidayCalendar_IsRecurring'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_HolidayCalendar_IsRecurring', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]') AND c.name = N'ConcurrencyStamp';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[LocationOperationalHour] ADD CONSTRAINT [DF_LocationOperationalHour_ConcurrencyStamp] DEFAULT NEWID() FOR [ConcurrencyStamp];
                ELSE IF @n <> N'DF_LocationOperationalHour_ConcurrencyStamp'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_LocationOperationalHour_ConcurrencyStamp', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]') AND c.name = N'DeferFinalMinutes';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[LocationOperationalHour] ADD CONSTRAINT [DF_LocationOperationalHour_DeferFinalMinutes] DEFAULT 30 FOR [DeferFinalMinutes];
                ELSE IF @n <> N'DF_LocationOperationalHour_DeferFinalMinutes'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_LocationOperationalHour_DeferFinalMinutes', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]') AND c.name = N'DeferNewTicketsOnFriday';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[LocationOperationalHour] ADD CONSTRAINT [DF_LocationOperationalHour_DeferOnFriday] DEFAULT 0 FOR [DeferNewTicketsOnFriday];
                ELSE IF @n <> N'DF_LocationOperationalHour_DeferOnFriday'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_LocationOperationalHour_DeferOnFriday', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[LocationOperationalHour]') AND c.name = N'IsRoundTheClock';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[LocationOperationalHour] ADD CONSTRAINT [DF_LocationOperationalHour_IsRoundTheClock] DEFAULT 0 FOR [IsRoundTheClock];
                ELSE IF @n <> N'DF_LocationOperationalHour_IsRoundTheClock'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_LocationOperationalHour_IsRoundTheClock', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[SlaPolicy]') AND c.name = N'ConcurrencyStamp';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[SlaPolicy] ADD CONSTRAINT [DF_SlaPolicy_ConcurrencyStamp] DEFAULT NEWID() FOR [ConcurrencyStamp];
                ELSE IF @n <> N'DF_SlaPolicy_ConcurrencyStamp'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SlaPolicy_ConcurrencyStamp', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[SlaPolicy]') AND c.name = N'NearDueWarningMinutes';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[SlaPolicy] ADD CONSTRAINT [DF_SlaPolicy_NearDueWarningMinutes] DEFAULT 30 FOR [NearDueWarningMinutes];
                ELSE IF @n <> N'DF_SlaPolicy_NearDueWarningMinutes'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SlaPolicy_NearDueWarningMinutes', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[SlaPolicy]') AND c.name = N'RespectHolidays';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[SlaPolicy] ADD CONSTRAINT [DF_SlaPolicy_RespectHolidays] DEFAULT 1 FOR [RespectHolidays];
                ELSE IF @n <> N'DF_SlaPolicy_RespectHolidays'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SlaPolicy_RespectHolidays', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[SlaPolicy]') AND c.name = N'RespectOperationalHours';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[SlaPolicy] ADD CONSTRAINT [DF_SlaPolicy_RespectOperationalHours] DEFAULT 1 FOR [RespectOperationalHours];
                ELSE IF @n <> N'DF_SlaPolicy_RespectOperationalHours'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SlaPolicy_RespectOperationalHours', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceLevel].[SlaPolicy]') AND c.name = N'RespectWeekends';
                IF @n IS NULL
                    ALTER TABLE [ServiceLevel].[SlaPolicy] ADD CONSTRAINT [DF_SlaPolicy_RespectWeekends] DEFAULT 1 FOR [RespectWeekends];
                ELSE IF @n <> N'DF_SlaPolicy_RespectWeekends'
                BEGIN
                    SET @q = N'[ServiceLevel].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SlaPolicy_RespectWeekends', @objtype = N'OBJECT';
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
