using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceDesk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceDesk_NamedDefaultConstraints : Migration
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
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ApprovalNotificationLog]') AND c.name = N'AttemptCount';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ApprovalNotificationLog] ADD CONSTRAINT [DF_ApprovalNotificationLog_AttemptCount] DEFAULT 0 FOR [AttemptCount];
                ELSE IF @n <> N'DF_ApprovalNotificationLog_AttemptCount'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ApprovalNotificationLog_AttemptCount', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ApprovalStageApproverRule]') AND c.name = N'IsRequired';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ApprovalStageApproverRule] ADD CONSTRAINT [DF_ApprovalStageApproverRule_IsRequired] DEFAULT 1 FOR [IsRequired];
                ELSE IF @n <> N'DF_ApprovalStageApproverRule_IsRequired'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ApprovalStageApproverRule_IsRequired', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]') AND c.name = N'IsDefault';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ApprovalWorkflowDefinition] ADD CONSTRAINT [DF_ApprovalWorkflowDefinition_IsDefault] DEFAULT 0 FOR [IsDefault];
                ELSE IF @n <> N'DF_ApprovalWorkflowDefinition_IsDefault'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ApprovalWorkflowDefinition_IsDefault', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowDefinition]') AND c.name = N'IsPublished';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ApprovalWorkflowDefinition] ADD CONSTRAINT [DF_ApprovalWorkflowDefinition_IsPublished] DEFAULT 0 FOR [IsPublished];
                ELSE IF @n <> N'DF_ApprovalWorkflowDefinition_IsPublished'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ApprovalWorkflowDefinition_IsPublished', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ApprovalWorkflowStage]') AND c.name = N'AllowDelegation';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ApprovalWorkflowStage] ADD CONSTRAINT [DF_ApprovalWorkflowStage_AllowDelegation] DEFAULT 0 FOR [AllowDelegation];
                ELSE IF @n <> N'DF_ApprovalWorkflowStage_AllowDelegation'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ApprovalWorkflowStage_AllowDelegation', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestEmail]') AND c.name = N'Direction';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestEmail] ADD CONSTRAINT [DF_RequestEmail_Direction] DEFAULT N'Outbound' FOR [Direction];
                ELSE IF @n <> N'DF_RequestEmail_Direction'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestEmail_Direction', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestEmail]') AND c.name = N'IsHtml';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestEmail] ADD CONSTRAINT [DF_RequestEmail_IsHtml] DEFAULT 1 FOR [IsHtml];
                ELSE IF @n <> N'DF_RequestEmail_IsHtml'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestEmail_IsHtml', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestHistory]') AND c.name = N'EntryKind';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestHistory] ADD CONSTRAINT [DF_RequestHistory_EntryKind] DEFAULT N'Transition' FOR [EntryKind];
                ELSE IF @n <> N'DF_RequestHistory_EntryKind'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestHistory_EntryKind', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestHistory]') AND c.name = N'IsInternal';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestHistory] ADD CONSTRAINT [DF_RequestHistory_IsInternal] DEFAULT 0 FOR [IsInternal];
                ELSE IF @n <> N'DF_RequestHistory_IsInternal'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestHistory_IsInternal', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestStatus]') AND c.name = N'CountsTechnicianTime';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestStatus] ADD CONSTRAINT [DF_RequestStatus_CountsTechnicianTime] DEFAULT 0 FOR [CountsTechnicianTime];
                ELSE IF @n <> N'DF_RequestStatus_CountsTechnicianTime'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestStatus_CountsTechnicianTime', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[RequestStatus]') AND c.name = N'SlaClockBehaviour';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[RequestStatus] ADD CONSTRAINT [DF_RequestStatus_SlaClockBehaviour] DEFAULT N'Running' FOR [SlaClockBehaviour];
                ELSE IF @n <> N'DF_RequestStatus_SlaClockBehaviour'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_RequestStatus_SlaClockBehaviour', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'IsScheduledHold';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_IsScheduledHold] DEFAULT 0 FOR [IsScheduledHold];
                ELSE IF @n <> N'DF_ServiceRequest_IsScheduledHold'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_IsScheduledHold', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'IsSlaOverdue';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_IsSlaOverdue] DEFAULT 0 FOR [IsSlaOverdue];
                ELSE IF @n <> N'DF_ServiceRequest_IsSlaOverdue'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_IsSlaOverdue', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'IsSlaPaused';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_IsSlaPaused] DEFAULT 0 FOR [IsSlaPaused];
                ELSE IF @n <> N'DF_ServiceRequest_IsSlaPaused'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_IsSlaPaused', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'ResolutionConsumedMinutes';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_ResolutionConsumedMinutes] DEFAULT 0 FOR [ResolutionConsumedMinutes];
                ELSE IF @n <> N'DF_ServiceRequest_ResolutionConsumedMinutes'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_ResolutionConsumedMinutes', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'SlaPausedMinutes';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_SlaPausedMinutes] DEFAULT 0 FOR [SlaPausedMinutes];
                ELSE IF @n <> N'DF_ServiceRequest_SlaPausedMinutes'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_SlaPausedMinutes', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceRequest]') AND c.name = N'TechnicianWorkingMinutes';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceRequest] ADD CONSTRAINT [DF_ServiceRequest_TechnicianWorkingMinutes] DEFAULT 0 FOR [TechnicianWorkingMinutes];
                ELSE IF @n <> N'DF_ServiceRequest_TechnicianWorkingMinutes'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceRequest_TechnicianWorkingMinutes', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[ServiceTemplate]') AND c.name = N'RequiresAsset';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[ServiceTemplate] ADD CONSTRAINT [DF_ServiceTemplate_RequiresAsset] DEFAULT 0 FOR [RequiresAsset];
                ELSE IF @n <> N'DF_ServiceTemplate_RequiresAsset'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_ServiceTemplate_RequiresAsset', @objtype = N'OBJECT';
                END
            ");
            migrationBuilder.Sql(@"
                DECLARE @n sysname, @q nvarchar(400);
                SELECT @n = dc.name FROM sys.default_constraints dc
                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                WHERE  dc.parent_object_id = OBJECT_ID(N'[ServiceDesk].[SupportTeam]') AND c.name = N'IsDefaultTeam';
                IF @n IS NULL
                    ALTER TABLE [ServiceDesk].[SupportTeam] ADD CONSTRAINT [DF_SupportTeam_IsDefaultTeam] DEFAULT 0 FOR [IsDefaultTeam];
                ELSE IF @n <> N'DF_SupportTeam_IsDefaultTeam'
                BEGIN
                    SET @q = N'[ServiceDesk].[' + @n + N']';
                    EXEC sp_rename @objname = @q, @newname = N'DF_SupportTeam_IsDefaultTeam', @objtype = N'OBJECT';
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
