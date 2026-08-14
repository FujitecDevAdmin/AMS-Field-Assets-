using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.ServiceLevel.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812_ServiceLevel_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ServiceLevel");

            migrationBuilder.CreateTable(
                name: "HolidayCalendar",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HolidayYear = table.Column<int>(type: "int", nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AppliesToAllLocations = table.Column<bool>(type: "bit", nullable: false),
                    IsRecurringAnnually = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    RecurrenceMonth = table.Column<byte>(type: "tinyint", nullable: true),
                    RecurrenceDay = table.Column<byte>(type: "tinyint", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendar", x => x.Id);
                    table.CheckConstraint("CK_HolidayCalendar_Recurrence", "([IsRecurringAnnually] = 0 OR ([RecurrenceMonth] BETWEEN 1 AND 12 AND [RecurrenceDay] >= 1 AND [RecurrenceDay] <= CASE WHEN [RecurrenceMonth] IN (4, 6, 9, 11) THEN 30 WHEN [RecurrenceMonth] = 2 THEN 29 ELSE 31 END))");
                    table.CheckConstraint("CK_HolidayCalendar_Type", "([HolidayType] IN (N'Government', N'Festival', N'Regional', N'Optional'))");
                    table.CheckConstraint("CK_HolidayCalendar_Year", "([HolidayYear] BETWEEN 2000 AND 2100)");
                    table.CheckConstraint("CK_HolidayCalendar_YearMatchesDate", "([HolidayYear] = YEAR([HolidayDate]))");
                });

            migrationBuilder.CreateTable(
                name: "LocationOperationalHour",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    IsRoundTheClock = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0"),
                    StandardStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    StandardEndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    DeferFinalMinutes = table.Column<int>(type: "int", nullable: false, defaultValueSql: "30"),
                    DeferNewTicketsOnFriday = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationOperationalHour", x => x.Id);
                    table.CheckConstraint("CK_LocationOperationalHour_BreakInside", "([IsRoundTheClock] = 1 OR [BreakStartTime] IS NULL OR ([BreakStartTime] >= [StandardStartTime] AND [BreakEndTime] <= [StandardEndTime]))");
                    table.CheckConstraint("CK_LocationOperationalHour_BreakPair", "(([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime]))");
                    table.CheckConstraint("CK_LocationOperationalHour_DeferMinutes", "([DeferFinalMinutes] BETWEEN 0 AND 480)");
                    table.CheckConstraint("CK_LocationOperationalHour_Window", "([IsRoundTheClock] = 1 OR [StandardEndTime] > [StandardStartTime])");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LocationOperationalHourHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ServiceLevel")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "SlaPolicy",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResponseTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    ResolutionTargetMinutes = table.Column<int>(type: "int", nullable: false),
                    RespectOperationalHours = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "1"),
                    RespectHolidays = table.Column<bool>(type: "bit", nullable: false),
                    RespectWeekends = table.Column<bool>(type: "bit", nullable: false),
                    NearDueWarningMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicy", x => x.Id);
                    table.CheckConstraint("CK_SlaPolicy_NearDue", "([NearDueWarningMinutes] >= 0)");
                    table.CheckConstraint("CK_SlaPolicy_Priority", "([Priority] IN (N'Low', N'Medium', N'High', N'Critical'))");
                    table.CheckConstraint("CK_SlaPolicy_ResponseWithinResolution", "([ResponseTargetMinutes] <= [ResolutionTargetMinutes])");
                    table.CheckConstraint("CK_SlaPolicy_Targets", "([ResponseTargetMinutes] > 0 AND [ResolutionTargetMinutes] > 0)");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SlaPolicyHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ServiceLevel")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "HolidayLocation",
                schema: "ServiceLevel",
                columns: table => new
                {
                    HolidayCalendarId = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayLocation", x => new { x.HolidayCalendarId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_HolidayLocation_HolidayCalendar_HolidayCalendarId",
                        column: x => x.HolidayCalendarId,
                        principalSchema: "ServiceLevel",
                        principalTable: "HolidayCalendar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationOperationalDay",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationOperationalHourId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "bit", nullable: false),
                    DayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakEndTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationOperationalDay", x => x.Id);
                    table.CheckConstraint("CK_LocationOperationalDay_CustomBreak", "(([BreakStartTime] IS NULL AND [BreakEndTime] IS NULL) OR ([BreakStartTime] IS NOT NULL AND [BreakEndTime] IS NOT NULL AND [BreakEndTime] > [BreakStartTime]))");
                    table.CheckConstraint("CK_LocationOperationalDay_CustomTimes", "([DayType] <> N'Custom' OR ([StartTime] IS NOT NULL AND [EndTime] IS NOT NULL AND [EndTime] > [StartTime]))");
                    table.CheckConstraint("CK_LocationOperationalDay_DayOfWeek", "([DayOfWeek] BETWEEN 0 AND 6)");
                    table.CheckConstraint("CK_LocationOperationalDay_DayType", "([DayType] IN (N'Standard', N'Custom', N'TwentyFourHour'))");
                    table.ForeignKey(
                        name: "FK_LocationOperationalDay_LocationOperationalHour_LocationOperationalHourId",
                        column: x => x.LocationOperationalHourId,
                        principalSchema: "ServiceLevel",
                        principalTable: "LocationOperationalHour",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationSaturdayRule",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationOperationalHourId = table.Column<int>(type: "int", nullable: false),
                    Occurrence = table.Column<byte>(type: "tinyint", nullable: false),
                    IsWorking = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationSaturdayRule", x => x.Id);
                    table.CheckConstraint("CK_LocationSaturdayRule_Occurrence", "([Occurrence] BETWEEN 1 AND 5)");
                    table.ForeignKey(
                        name: "FK_LocationSaturdayRule_LocationOperationalHour_LocationOperationalHourId",
                        column: x => x.LocationOperationalHourId,
                        principalSchema: "ServiceLevel",
                        principalTable: "LocationOperationalHour",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaEscalation",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SlaPolicyId = table.Column<int>(type: "int", nullable: false),
                    EscalationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ThresholdPercent = table.Column<int>(type: "int", nullable: false),
                    RecipientType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaEscalation", x => x.Id);
                    table.CheckConstraint("CK_SlaEscalation_Channel", "([Channel] IN (N'Email', N'InApp', N'Both'))");
                    table.CheckConstraint("CK_SlaEscalation_CustomAddress", "([RecipientType] <> N'Custom' OR [RecipientAddress] IS NOT NULL)");
                    table.CheckConstraint("CK_SlaEscalation_Level", "([Level] BETWEEN 1 AND 4)");
                    table.CheckConstraint("CK_SlaEscalation_RecipientType", "([RecipientType] IN (N'AssignedTechnician', N'TeamLead', N'BranchAdmin', N'Manager', N'Custom'))");
                    table.CheckConstraint("CK_SlaEscalation_Threshold", "([ThresholdPercent] BETWEEN 1 AND 1000)");
                    table.CheckConstraint("CK_SlaEscalation_Type", "([EscalationType] IN (N'Response', N'Resolution'))");
                    table.ForeignKey(
                        name: "FK_SlaEscalation_SlaPolicy_SlaPolicyId",
                        column: x => x.SlaPolicyId,
                        principalSchema: "ServiceLevel",
                        principalTable: "SlaPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaEscalationLog",
                schema: "ServiceLevel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    SlaEscalationId = table.Column<int>(type: "int", nullable: false),
                    EscalationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SentTo = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmailOutboxId = table.Column<long>(type: "bigint", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FiredOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaEscalationLog", x => x.Id);
                    table.CheckConstraint("CK_SlaEscalationLog_Outcome", "([Outcome] IN (N'Queued', N'Sent', N'Failed', N'Skipped'))");
                    table.ForeignKey(
                        name: "FK_SlaEscalationLog_SlaEscalation_SlaEscalationId",
                        column: x => x.SlaEscalationId,
                        principalSchema: "ServiceLevel",
                        principalTable: "SlaEscalation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_Recurring",
                schema: "ServiceLevel",
                table: "HolidayCalendar",
                columns: new[] { "RecurrenceMonth", "RecurrenceDay" },
                filter: "[IsRecurringAnnually] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayCalendar_YearDate",
                schema: "ServiceLevel",
                table: "HolidayCalendar",
                columns: new[] { "HolidayYear", "HolidayDate" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayLocation_LocationId",
                schema: "ServiceLevel",
                table: "HolidayLocation",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "UX_LocationOperationalDay_Day",
                schema: "ServiceLevel",
                table: "LocationOperationalDay",
                columns: new[] { "LocationOperationalHourId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LocationOperationalHour_Location",
                schema: "ServiceLevel",
                table: "LocationOperationalHour",
                column: "LocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LocationSaturdayRule_Occurrence",
                schema: "ServiceLevel",
                table: "LocationSaturdayRule",
                columns: new[] { "LocationOperationalHourId", "Occurrence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SlaEscalation_PolicyTypeLevel",
                schema: "ServiceLevel",
                table: "SlaEscalation",
                columns: new[] { "SlaPolicyId", "EscalationType", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaEscalationLog_Request",
                schema: "ServiceLevel",
                table: "SlaEscalationLog",
                columns: new[] { "ServiceRequestId", "FiredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SlaEscalationLog_OncePerLevel",
                schema: "ServiceLevel",
                table: "SlaEscalationLog",
                columns: new[] { "ServiceRequestId", "SlaEscalationId" },
                unique: true,
                filter: "[Outcome] <> N'Failed'");

            migrationBuilder.CreateIndex(
                name: "UX_SlaPolicy_ActivePriority",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                column: "Priority",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_SlaPolicy_Name",
                schema: "ServiceLevel",
                table: "SlaPolicy",
                column: "PolicyName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayLocation",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "LocationOperationalDay",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "LocationSaturdayRule",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "SlaEscalationLog",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "HolidayCalendar",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "LocationOperationalHour",
                schema: "ServiceLevel")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "LocationOperationalHourHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ServiceLevel")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropTable(
                name: "SlaEscalation",
                schema: "ServiceLevel");

            migrationBuilder.DropTable(
                name: "SlaPolicy",
                schema: "ServiceLevel")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SlaPolicyHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "ServiceLevel")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
