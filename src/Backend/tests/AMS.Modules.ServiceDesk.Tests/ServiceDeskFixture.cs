using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

/// <summary>A caller the tests own.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public int Id { get; set; } = 1;

    public string Username { get; set; } = "test-admin";

    public int? EmployeeId { get; set; }

    public bool HasAllBranches { get; set; } = true;

    public IReadOnlySet<int> BranchIds { get; set; } = new HashSet<int>();

    public IReadOnlySet<string> Capabilities { get; set; } = new HashSet<string>();
}

/// <summary>The ServiceDesk schema, built by the module's own migrations.</summary>
public sealed class ServiceDeskFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_ServiceDeskTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>
    /// ServiceLevel's answers. Defaults to "no policy configured", which is a
    /// real site's starting state.
    /// </summary>
    public FakeSlaCalculator Sla { get; } = new();

    /// <summary>The outbox: what this module asked to have sent.</summary>
    public FakeNotifier Notifier { get; } = new();

    /// <summary>
    /// Identity and Organization, as far as approval routing is concerned.
    /// </summary>
    /// <remarks>
    /// Seeded with the people the approval tests route to. Reset does not
    /// clear them: they stand for records in two other modules' databases,
    /// which this module's tests do not own and cannot empty.
    /// </remarks>
    public FakeUserDirectory Users { get; } = new FakeUserDirectory()
        .With(1, "Test Admin", "admin@fujitec.co.in", employeeId: 1)
        .With(20, "R Kumar", "kumar@fujitec.co.in", employeeId: 200)
        .With(30, "S Iyer", "iyer@fujitec.co.in", employeeId: 300);

    public FakeEmployeeDirectory Employees { get; } = new FakeEmployeeDirectory()
        .Reports(100, 200)
        .Reports(200, 300)
        .At(100, 2);

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_RequestStatus_Name", "RequestStatus.NameTaken",
            "A ticket status with that name already exists.")
        .Register("UX_RequestCategory_Name", "RequestCategory.NameTaken",
            "A category with that name already exists.")
        .Register("UX_RequestSubCategory_Name", "RequestSubCategory.NameTaken",
            "That category already has a sub-category with this name.")
        .Register("UX_SupportTeam_Name", "SupportTeam.NameTaken",
            "A team with that name already exists.")
        .Register("UX_SupportTeam_OneDefault", "SupportTeam.DefaultExists",
            "Another team is already the default. Clear that one first.")
        .Register("UX_ServiceTemplate_Name", "ServiceTemplate.NameTaken",
            "A template with that name already exists.")
        .Register("UX_ServiceRequest_Number", "ServiceRequest.NumberTaken",
            "That ticket number is already in use.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();

        await SeedStatusesAsync();
    }

    /// <summary>
    /// The ten ticket statuses, exactly as section 17.2 of the design script
    /// seeds them.
    /// </summary>
    /// <remarks>
    /// The migrations create the table and nothing else — reference data lives
    /// in the design script, not in <c>HasData</c>. A database built purely
    /// from migrations therefore has no statuses at all, and no ticket can be
    /// raised in it. That is a real deployment gap, recorded in
    /// docs/00DESIGNDECISIONS.md; here it is enough that the tests stand up
    /// the same ten rows production gets.
    ///
    /// Seeded once, in <see cref="InitializeAsync"/> rather than in
    /// <see cref="ResetAsync"/>: statuses are the vocabulary, not the data
    /// under test, and their ids stay stable across every test in the class.
    /// </remarks>
    private async Task SeedStatusesAsync() => await ExecuteAsync("""
        INSERT INTO [ServiceDesk].[RequestStatus]
            ([StatusName], [IsClosedState], [DisplayOrder], [IsActive],
             [SlaClockBehaviour], [CountsTechnicianTime], [CreatedOnUtc], [CreatedBy])
        VALUES
            (N'Open',              0,  1, 1, N'Running', 0, SYSUTCDATETIME(), N'test'),
            (N'Assigned',          0,  2, 1, N'Running', 0, SYSUTCDATETIME(), N'test'),
            (N'In Progress',       0,  3, 1, N'Running', 1, SYSUTCDATETIME(), N'test'),
            (N'On Hold',           0,  4, 1, N'Paused',  0, SYSUTCDATETIME(), N'test'),
            (N'Waiting for User',  0,  5, 1, N'Paused',  0, SYSUTCDATETIME(), N'test'),
            (N'Waiting for Spare', 0,  6, 1, N'Paused',  0, SYSUTCDATETIME(), N'test'),
            (N'Standby Provided',  0,  7, 1, N'Running', 0, SYSUTCDATETIME(), N'test'),
            (N'Resolved',          0,  8, 1, N'Stopped', 0, SYSUTCDATETIME(), N'test'),
            (N'Closed',            1,  9, 1, N'Stopped', 0, SYSUTCDATETIME(), N'test'),
            (N'Rejected',          1, 10, 1, N'Stopped', 0, SYSUTCDATETIME(), N'test');
        """);

    /// <summary>The id of a seeded status, by the name section 17.2 gives it.</summary>
    public async Task<int> StatusIdAsync(string statusName)
    {
        await using var context = NewContext();

        return await context.RequestStatuses
            .Where(s => s.StatusName == statusName)
            .Select(s => s.Id)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public ServiceDeskDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", ServiceDeskDbContext.SchemaName))
            .Options);

    /// <summary>
    /// Empties everything except the statuses. Children first: the ticket
    /// tables cascade, but the e-mail rows history points at do not (R2-6 gave
    /// RequestHistory.RequestEmailId a real NO ACTION foreign key precisely so
    /// an e-mail cannot vanish out from under the entry describing it).
    /// </summary>
    public async Task ResetAsync()
    {
        Sla.Reset();
        Notifier.Reset();
        await EmptyAsync();
    }

    private async Task EmptyAsync() => await ExecuteAsync("""
        DELETE FROM [ServiceDesk].[ApprovalNotificationLog];
        DELETE FROM [ServiceDesk].[RequestApprovalDecision];
        DELETE FROM [ServiceDesk].[RequestApprovalParticipant];
        DELETE FROM [ServiceDesk].[RequestApprovalStep];
        DELETE FROM [ServiceDesk].[RequestApprovalInstance];
        DELETE FROM [ServiceDesk].[ApprovalStageApproverRule];
        DELETE FROM [ServiceDesk].[ApprovalWorkflowStage];
        DELETE FROM [ServiceDesk].[ApprovalWorkflowDefinition];
        DELETE FROM [ServiceDesk].[RequestAttachment];
        DELETE FROM [ServiceDesk].[RequestHistory];
        DELETE FROM [ServiceDesk].[RequestEmail];
        DELETE FROM [ServiceDesk].[NewServiceRequestItem];
        DELETE FROM [ServiceDesk].[NewServiceRequestDetail];
        DELETE FROM [ServiceDesk].[ServiceRequest];
        DELETE FROM [ServiceDesk].[ServiceTemplate];
        DELETE FROM [ServiceDesk].[SupportTeamMember];
        DELETE FROM [ServiceDesk].[SupportTeam];
        DELETE FROM [ServiceDesk].[RequestSubCategory];
        DELETE FROM [ServiceDesk].[RequestCategory];
        """);

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync() => await ExecuteOnMasterAsync($"""
        IF DB_ID('{Database}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{Database}];
        END
        """);

    private static async Task ExecuteOnMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(
            $"Server={Instance};Database=master;Integrated Security=true;TrustServerCertificate=true");
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(ServiceDeskCollectionDefinition))]
public sealed class ServiceDeskCollectionDefinition : ICollectionFixture<ServiceDeskFixture>;
