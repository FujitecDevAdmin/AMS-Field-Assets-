using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceDesk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceDesk_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ServiceDesk");

            migrationBuilder.CreateTable(
                name: "RequestCategory",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatus",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsClosedState = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SlaClockBehaviour = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "N'Running'"),
                    CountsTechnicianTime = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatus", x => x.Id);
                    table.CheckConstraint("CK_RequestStatus_SlaClockBehaviour", "([SlaClockBehaviour] IN (N'Running', N'Paused', N'Stopped'))");
                });

            migrationBuilder.CreateTable(
                name: "SupportTeam",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegionId = table.Column<int>(type: "int", nullable: true),
                    MailboxAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDefaultTeam = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTeam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestSubCategory",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestCategoryId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestSubCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestSubCategory_RequestCategory_RequestCategoryId",
                        column: x => x.RequestCategoryId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportTeamMember",
                schema: "ServiceDesk",
                columns: table => new
                {
                    SupportTeamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsLead = table.Column<bool>(type: "bit", nullable: false),
                    AddedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTeamMember", x => new { x.SupportTeamId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SupportTeamMember_SupportTeam_SupportTeamId",
                        column: x => x.SupportTeamId,
                        principalSchema: "ServiceDesk",
                        principalTable: "SupportTeam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTemplate",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RequestKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestCategoryId = table.Column<int>(type: "int", nullable: true),
                    RequestSubCategoryId = table.Column<int>(type: "int", nullable: true),
                    DefaultPriority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultSupportTeamId = table.Column<int>(type: "int", nullable: true),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequiresAsset = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTemplate", x => x.Id);
                    table.CheckConstraint("CK_ServiceTemplate_Kind", "([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService'))");
                    table.CheckConstraint("CK_ServiceTemplate_Priority", "([DefaultPriority] IN (N'Low', N'Medium', N'High', N'Critical'))");
                    table.ForeignKey(
                        name: "FK_ServiceTemplate_RequestCategory_RequestCategoryId",
                        column: x => x.RequestCategoryId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceTemplate_RequestSubCategory_RequestSubCategoryId",
                        column: x => x.RequestSubCategoryId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestSubCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceTemplate_SupportTeam_DefaultSupportTeamId",
                        column: x => x.DefaultSupportTeamId,
                        principalSchema: "ServiceDesk",
                        principalTable: "SupportTeam",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowDefinition",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceTemplateId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowDefinition", x => x.Id);
                    table.CheckConstraint("CK_ApprovalWorkflowDefinition_EffectiveRange", "([EffectiveToUtc] IS NULL OR [EffectiveFromUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc])");
                    table.CheckConstraint("CK_ApprovalWorkflowDefinition_Priority", "([Priority] IS NULL OR [Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
                    table.CheckConstraint("CK_ApprovalWorkflowDefinition_Version", "([VersionNumber] > 0)");
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowDefinition_ServiceTemplate_ServiceTemplateId",
                        column: x => x.ServiceTemplateId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceRequest",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestStatusId = table.Column<int>(type: "int", nullable: false),
                    RequestCategoryId = table.Column<int>(type: "int", nullable: true),
                    RequestSubCategoryId = table.Column<int>(type: "int", nullable: true),
                    ServiceTemplateId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    ManualAssetText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    OnBehalfOfEmployeeId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    AssignedTeamId = table.Column<int>(type: "int", nullable: true),
                    AssignedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SlaPolicyId = table.Column<int>(type: "int", nullable: true),
                    SlaStartOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsScheduledHold = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    NextOperationalStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScheduleHoldReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ResponseDueOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionDueOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstResponseOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseElapsedMinutes = table.Column<int>(type: "int", nullable: true),
                    ResolutionConsumedMinutes = table.Column<int>(type: "int", nullable: false, defaultValueSql: "0"),
                    TechnicianWorkingMinutes = table.Column<int>(type: "int", nullable: false, defaultValueSql: "0"),
                    SlaPausedMinutes = table.Column<int>(type: "int", nullable: false, defaultValueSql: "0"),
                    SlaLastCalculatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSlaPaused = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    IsSlaOverdue = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRequest", x => x.Id);
                    table.CheckConstraint("CK_ServiceRequest_Kind", "([RequestKind] IN (N'SupportTicket', N'AssetIssue', N'NewService'))");
                    table.CheckConstraint("CK_ServiceRequest_Priority", "([Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
                    table.CheckConstraint("CK_ServiceRequest_ScheduledHold", "([IsScheduledHold] = 0 OR [NextOperationalStartUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_ServiceRequest_SlaMinutes", "([ResolutionConsumedMinutes] >= 0 AND [TechnicianWorkingMinutes] >= 0 AND [SlaPausedMinutes] >= 0 AND ([ResponseElapsedMinutes] IS NULL OR [ResponseElapsedMinutes] >= 0))");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_RequestCategory_RequestCategoryId",
                        column: x => x.RequestCategoryId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_RequestStatus_RequestStatusId",
                        column: x => x.RequestStatusId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_RequestSubCategory_RequestSubCategoryId",
                        column: x => x.RequestSubCategoryId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestSubCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_ServiceTemplate_ServiceTemplateId",
                        column: x => x.ServiceTemplateId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceRequest_SupportTeam_AssignedTeamId",
                        column: x => x.AssignedTeamId,
                        principalSchema: "ServiceDesk",
                        principalTable: "SupportTeam",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflowStage",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalWorkflowId = table.Column<int>(type: "int", nullable: false),
                    StageNumber = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApprovalMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DueAfterMinutes = table.Column<int>(type: "int", nullable: true),
                    ReminderAfterMinutes = table.Column<int>(type: "int", nullable: true),
                    ReminderRepeatMinutes = table.Column<int>(type: "int", nullable: true),
                    EscalateAfterMinutes = table.Column<int>(type: "int", nullable: true),
                    AllowDelegation = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflowStage", x => x.Id);
                    table.CheckConstraint("CK_ApprovalWorkflowStage_Mode", "([ApprovalMode] IN (N'Any', N'All'))");
                    table.CheckConstraint("CK_ApprovalWorkflowStage_Number", "([StageNumber] > 0)");
                    table.CheckConstraint("CK_ApprovalWorkflowStage_Timers", "( ([DueAfterMinutes] IS NULL OR [DueAfterMinutes] > 0) AND ([ReminderAfterMinutes] IS NULL OR [ReminderAfterMinutes] > 0) AND ([ReminderRepeatMinutes] IS NULL OR [ReminderRepeatMinutes] > 0) AND ([EscalateAfterMinutes] IS NULL OR [EscalateAfterMinutes] >= 0) )");
                    table.ForeignKey(
                        name: "FK_ApprovalWorkflowStage_ApprovalWorkflowDefinition_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ApprovalWorkflowDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewServiceRequestDetail",
                schema: "ServiceDesk",
                columns: table => new
                {
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    NeedsEmail = table.Column<bool>(type: "bit", nullable: false),
                    NeedsErp = table.Column<bool>(type: "bit", nullable: false),
                    NeedsDms = table.Column<bool>(type: "bit", nullable: false),
                    NeedsVpn = table.Column<bool>(type: "bit", nullable: false),
                    RequiredByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewServiceRequestDetail", x => x.ServiceRequestId);
                    table.ForeignKey(
                        name: "FK_NewServiceRequestDetail_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewServiceRequestItem",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewServiceRequestItem", x => x.Id);
                    table.CheckConstraint("CK_NewServiceRequestItem_PositiveQuantity", "([Quantity] > 0)");
                    table.ForeignKey(
                        name: "FK_NewServiceRequestItem_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovalInstance",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    ApprovalWorkflowId = table.Column<int>(type: "int", nullable: false),
                    WorkflowNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    WorkflowVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStageNumber = table.Column<int>(type: "int", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: false),
                    SubmittedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUserId = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovalInstance", x => x.Id);
                    table.CheckConstraint("CK_RequestApprovalInstance_CurrentStage", "([CurrentStageNumber] IS NULL OR [CurrentStageNumber] > 0)");
                    table.CheckConstraint("CK_RequestApprovalInstance_Status", "([Status] IN (N'Pending', N'Approved', N'Rejected', N'Cancelled'))");
                    table.CheckConstraint("CK_RequestApprovalInstance_Version", "([WorkflowVersion] > 0)");
                    table.ForeignKey(
                        name: "FK_RequestApprovalInstance_ApprovalWorkflowDefinition_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ApprovalWorkflowDefinition",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestApprovalInstance_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequestEmail",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValueSql: "N'Outbound'"),
                    ToAddresses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CcAddresses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHtml = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    SentByUserId = table.Column<int>(type: "int", nullable: true),
                    QueuedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestEmail", x => x.Id);
                    table.CheckConstraint("CK_RequestEmail_Direction", "([Direction] IN (N'Outbound', N'Inbound'))");
                    table.CheckConstraint("CK_RequestEmail_SentBy", "([Direction] = N'Inbound' OR [SentByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_RequestEmail_Status", "([Status] IN (N'Queued', N'Sent', N'Failed'))");
                    table.ForeignKey(
                        name: "FK_RequestEmail_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalStageApproverRule",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalWorkflowStageId = table.Column<int>(type: "int", nullable: false),
                    ResolverType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResolverUserId = table.Column<int>(type: "int", nullable: true),
                    ResolverRoleId = table.Column<int>(type: "int", nullable: true),
                    ResolverCapabilityName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ResolverEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalStageApproverRule", x => x.Id);
                    table.CheckConstraint("CK_ApprovalStageApproverRule_ResolverType", "([ResolverType] IN ( N'User', N'Role', N'Capability', N'EmployeeManager', N'RequesterManager', N'LocationBranchAdmin', N'CustomEmail' ))");
                    table.CheckConstraint("CK_ApprovalStageApproverRule_Value", "( ([ResolverType] = N'User' AND [ResolverUserId] IS NOT NULL) OR ([ResolverType] = N'Role' AND [ResolverRoleId] IS NOT NULL) OR ([ResolverType] = N'Capability' AND [ResolverCapabilityName] IS NOT NULL) OR ([ResolverType] = N'EmployeeManager') OR ([ResolverType] = N'RequesterManager') OR ([ResolverType] = N'LocationBranchAdmin' AND [ResolverCapabilityName] IS NOT NULL) OR ([ResolverType] = N'CustomEmail' AND [ResolverEmail] IS NOT NULL) )");
                    table.ForeignKey(
                        name: "FK_ApprovalStageApproverRule_ApprovalWorkflowStage_ApprovalWorkflowStageId",
                        column: x => x.ApprovalWorkflowStageId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ApprovalWorkflowStage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovalStep",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestApprovalInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    ApprovalWorkflowStageId = table.Column<int>(type: "int", nullable: false),
                    StageNumber = table.Column<int>(type: "int", nullable: false),
                    StageNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApprovalModeSnapshot = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActivatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutcomeRemarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovalStep", x => x.Id);
                    table.CheckConstraint("CK_RequestApprovalStep_Activation", "([Status] IN (N'Waiting', N'Cancelled', N'Skipped') OR [ActivatedOnUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_RequestApprovalStep_Mode", "([ApprovalModeSnapshot] IN (N'Any', N'All'))");
                    table.CheckConstraint("CK_RequestApprovalStep_Number", "([StageNumber] > 0)");
                    table.CheckConstraint("CK_RequestApprovalStep_Status", "([Status] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Skipped', N'Cancelled'))");
                    table.ForeignKey(
                        name: "FK_RequestApprovalStep_ApprovalWorkflowStage_ApprovalWorkflowStageId",
                        column: x => x.ApprovalWorkflowStageId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ApprovalWorkflowStage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestApprovalStep_RequestApprovalInstance_RequestApprovalInstanceId",
                        column: x => x.RequestApprovalInstanceId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachment",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    RequestEmailId = table.Column<int>(type: "int", nullable: true),
                    AttachmentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    UploadedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachment", x => x.Id);
                    table.CheckConstraint("CK_RequestAttachment_Type", "([AttachmentType] IN (N'Requester', N'Resolution', N'Email'))");
                    table.ForeignKey(
                        name: "FK_RequestAttachment_RequestEmail_RequestEmailId",
                        column: x => x.RequestEmailId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestEmail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestAttachment_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestHistory",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    EntryKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValueSql: "N'Transition'"),
                    EntryText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    FromStatusId = table.Column<int>(type: "int", nullable: true),
                    ToStatusId = table.Column<int>(type: "int", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true),
                    RequestEmailId = table.Column<int>(type: "int", nullable: true),
                    OccurredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestHistory", x => x.Id);
                    table.CheckConstraint("CK_RequestHistory_EntryKind", "([EntryKind] IN (N'Transition', N'Note', N'Email', N'Automation', N'Sla', N'Escalation'))");
                    table.ForeignKey(
                        name: "FK_RequestHistory_RequestEmail_RequestEmailId",
                        column: x => x.RequestEmailId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestEmail",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestHistory_ServiceRequest_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ServiceRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovalParticipant",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestApprovalStepId = table.Column<long>(type: "bigint", nullable: false),
                    ApproverRuleId = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ApproverEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ApproverNameSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApproverEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ParticipantStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DelegatedToUserId = table.Column<int>(type: "int", nullable: true),
                    DelegatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovalParticipant", x => x.Id);
                    table.CheckConstraint("CK_RequestApprovalParticipant_Identity", "([ApproverUserId] IS NOT NULL OR [ApproverEmployeeId] IS NOT NULL OR [ApproverEmailSnapshot] <> N'')");
                    table.CheckConstraint("CK_RequestApprovalParticipant_Status", "([ParticipantStatus] IN (N'Waiting', N'Pending', N'Approved', N'Rejected', N'Delegated', N'Cancelled'))");
                    table.ForeignKey(
                        name: "FK_RequestApprovalParticipant_ApprovalStageApproverRule_ApproverRuleId",
                        column: x => x.ApproverRuleId,
                        principalSchema: "ServiceDesk",
                        principalTable: "ApprovalStageApproverRule",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestApprovalParticipant_RequestApprovalStep_RequestApprovalStepId",
                        column: x => x.RequestApprovalStepId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalStep",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalNotificationLog",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestApprovalInstanceId = table.Column<long>(type: "bigint", nullable: false),
                    RequestApprovalStepId = table.Column<long>(type: "bigint", nullable: true),
                    RequestApprovalParticipantId = table.Column<long>(type: "bigint", nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SubjectSnapshot = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EmailOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValueSql: "0"),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QueuedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalNotificationLog", x => x.Id);
                    table.CheckConstraint("CK_ApprovalNotificationLog_Attempts", "([AttemptCount] >= 0)");
                    table.CheckConstraint("CK_ApprovalNotificationLog_Status", "([Status] IN (N'Queued', N'Sent', N'Failed', N'Skipped'))");
                    table.CheckConstraint("CK_ApprovalNotificationLog_Type", "([NotificationType] IN ( N'ApprovalRequired', N'Reminder', N'Escalation', N'StepApproved', N'RequestApproved', N'RequestRejected', N'RequestCancelled' ))");
                    table.ForeignKey(
                        name: "FK_ApprovalNotificationLog_RequestApprovalInstance_RequestApprovalInstanceId",
                        column: x => x.RequestApprovalInstanceId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalInstance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalNotificationLog_RequestApprovalParticipant_RequestApprovalParticipantId",
                        column: x => x.RequestApprovalParticipantId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalParticipant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalNotificationLog_RequestApprovalStep_RequestApprovalStepId",
                        column: x => x.RequestApprovalStepId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalStep",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovalDecision",
                schema: "ServiceDesk",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestApprovalParticipantId = table.Column<long>(type: "bigint", nullable: false),
                    ClientDecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActedByUserId = table.Column<int>(type: "int", nullable: true),
                    ActedByEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovalDecision", x => x.Id);
                    table.CheckConstraint("CK_RequestApprovalDecision_Decision", "([Decision] IN (N'Approved', N'Rejected'))");
                    table.CheckConstraint("CK_RequestApprovalDecision_Source", "([Source] IN (N'Application', N'EmailLink', N'Api'))");
                    table.ForeignKey(
                        name: "FK_RequestApprovalDecision_RequestApprovalParticipant_RequestApprovalParticipantId",
                        column: x => x.RequestApprovalParticipantId,
                        principalSchema: "ServiceDesk",
                        principalTable: "RequestApprovalParticipant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotificationLog_Instance",
                schema: "ServiceDesk",
                table: "ApprovalNotificationLog",
                columns: new[] { "RequestApprovalInstanceId", "QueuedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotificationLog_Outbox",
                schema: "ServiceDesk",
                table: "ApprovalNotificationLog",
                column: "EmailOutboxId",
                filter: "[EmailOutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalNotificationLog_Idempotency",
                schema: "ServiceDesk",
                table: "ApprovalNotificationLog",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalStageApproverRule_Stage",
                schema: "ServiceDesk",
                table: "ApprovalStageApproverRule",
                columns: new[] { "ApprovalWorkflowStageId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflowDefinition_Match",
                schema: "ServiceDesk",
                table: "ApprovalWorkflowDefinition",
                columns: new[] { "ServiceTemplateId", "LocationId", "Priority", "EffectiveFromUtc" },
                filter: "[IsActive] = 1 AND [IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalWorkflowDefinition_NameVersion",
                schema: "ServiceDesk",
                table: "ApprovalWorkflowDefinition",
                columns: new[] { "WorkflowName", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalWorkflowDefinition_OneActiveDefault",
                schema: "ServiceDesk",
                table: "ApprovalWorkflowDefinition",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalWorkflowStage_Number",
                schema: "ServiceDesk",
                table: "ApprovalWorkflowStage",
                columns: new[] { "ApprovalWorkflowId", "StageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewServiceRequestItem_ServiceRequestId",
                schema: "ServiceDesk",
                table: "NewServiceRequestItem",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalDecision_ClientId",
                schema: "ServiceDesk",
                table: "RequestApprovalDecision",
                column: "ClientDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalDecision_Participant",
                schema: "ServiceDesk",
                table: "RequestApprovalDecision",
                column: "RequestApprovalParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovalInstance_Request",
                schema: "ServiceDesk",
                table: "RequestApprovalInstance",
                columns: new[] { "ServiceRequestId", "SubmittedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalInstance_OnePending",
                schema: "ServiceDesk",
                table: "RequestApprovalInstance",
                column: "ServiceRequestId",
                unique: true,
                filter: "[Status] = N'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovalParticipant_Inbox",
                schema: "ServiceDesk",
                table: "RequestApprovalParticipant",
                columns: new[] { "ApproverUserId", "ParticipantStatus", "RequestApprovalStepId" });

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalParticipant_Resolved",
                schema: "ServiceDesk",
                table: "RequestApprovalParticipant",
                columns: new[] { "RequestApprovalStepId", "ApproverRuleId", "ApproverEmailSnapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovalStep_Due",
                schema: "ServiceDesk",
                table: "RequestApprovalStep",
                column: "DueOnUtc",
                filter: "[Status] = N'Pending'");

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalStep_Number",
                schema: "ServiceDesk",
                table: "RequestApprovalStep",
                columns: new[] { "RequestApprovalInstanceId", "StageNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RequestApprovalStep_OnePending",
                schema: "ServiceDesk",
                table: "RequestApprovalStep",
                column: "RequestApprovalInstanceId",
                unique: true,
                filter: "[Status] = N'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachment_ServiceRequestId",
                schema: "ServiceDesk",
                table: "RequestAttachment",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "UX_RequestCategory_Name",
                schema: "ServiceDesk",
                table: "RequestCategory",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestEmail_Request",
                schema: "ServiceDesk",
                table: "RequestEmail",
                columns: new[] { "ServiceRequestId", "QueuedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_Request",
                schema: "ServiceDesk",
                table: "RequestHistory",
                columns: new[] { "ServiceRequestId", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestHistory_RequestEmailId",
                schema: "ServiceDesk",
                table: "RequestHistory",
                column: "RequestEmailId");

            migrationBuilder.CreateIndex(
                name: "UX_RequestStatus_Name",
                schema: "ServiceDesk",
                table: "RequestStatus",
                column: "StatusName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RequestSubCategory_Name",
                schema: "ServiceDesk",
                table: "RequestSubCategory",
                columns: new[] { "RequestCategoryId", "SubCategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_AssignedTeam",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                columns: new[] { "AssignedTeamId", "RequestStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_Queue",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                columns: new[] { "RequestStatusId", "LocationId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_Requester",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_ScheduledIntake",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                column: "NextOperationalStartUtc",
                filter: "[IsScheduledHold] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequest_SlaQueue",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                columns: new[] { "IsSlaOverdue", "ResolutionDueOnUtc" },
                filter: "[ClosedOnUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceRequest_Number",
                schema: "ServiceDesk",
                table: "ServiceRequest",
                column: "RequestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ServiceTemplate_Name",
                schema: "ServiceDesk",
                table: "ServiceTemplate",
                column: "TemplateName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTeam_RegionId",
                schema: "ServiceDesk",
                table: "SupportTeam",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "UX_SupportTeam_Name",
                schema: "ServiceDesk",
                table: "SupportTeam",
                column: "TeamName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SupportTeam_OneDefault",
                schema: "ServiceDesk",
                table: "SupportTeam",
                column: "IsDefaultTeam",
                unique: true,
                filter: "[IsDefaultTeam] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalNotificationLog",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "NewServiceRequestDetail",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "NewServiceRequestItem",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestApprovalDecision",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestAttachment",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestHistory",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "SupportTeamMember",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestApprovalParticipant",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestEmail",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "ApprovalStageApproverRule",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestApprovalStep",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowStage",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestApprovalInstance",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflowDefinition",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "ServiceRequest",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestStatus",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "ServiceTemplate",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestSubCategory",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "SupportTeam",
                schema: "ServiceDesk");

            migrationBuilder.DropTable(
                name: "RequestCategory",
                schema: "ServiceDesk");
        }
    }
}
