using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    /// <summary>A Wednesday morning, well inside a working week.</summary>
    public static readonly DateTime Default = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; } = Default;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);

    /// <summary>Back to the start.</summary>
    /// <remarks>
    /// The fixture is shared across every test class in the collection, so a
    /// test that moves the clock leaves it moved. One that then computes a due
    /// date from "now" is still self-consistent, but one that sets an absolute
    /// date is not — and the pair only fail together, in an order that depends
    /// on how the runner feels.
    /// </remarks>
    public void Reset() => UtcNow = Default;
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

/// <summary>
/// Organization's branches, as far as this module is concerned.
/// </summary>
/// <remarks>
/// A stub, because ServiceLevel should be testable without an Organization
/// database standing behind it. What it needs is a time zone and whether the
/// branch exists; both are answers, not tables.
/// </remarks>
public sealed class FakeLocationDirectory : ILocationDirectory
{
    private readonly Dictionary<int, string> _zones = new()
    {
        [1] = "India Standard Time",
        [2] = "India Standard Time",
        [3] = "GMT Standard Time",
    };

    public FakeLocationDirectory With(int locationId, string timeZoneId)
    {
        _zones[locationId] = timeZoneId;

        return this;
    }

    public Task<string?> TimeZoneOfAsync(int locationId, CancellationToken ct) =>
        Task.FromResult(_zones.TryGetValue(locationId, out var zone) ? zone : null);

    public Task<bool> IsActiveAsync(int locationId, CancellationToken ct) =>
        Task.FromResult(_zones.ContainsKey(locationId));
}

/// <summary>The ServiceLevel schema, built by the module's own migrations.</summary>
public sealed class ServiceLevelFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_ServiceLevelTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public FakeLocationDirectory Locations { get; } = new();

    /// <summary>ServiceDesk's tickets, as far as the escalation monitor sees them.</summary>
    public FakeSlaWatchList Tickets { get; } = new();

    /// <summary>The outbox: what the monitor asked to have sent.</summary>
    public FakeNotifier Notifier { get; } = new();

    /// <summary>
    /// The people an escalation can reach.
    /// </summary>
    /// <remarks>
    /// Reset does not clear them: they stand for records in two other modules'
    /// databases, which this module's tests do not own and cannot empty.
    /// </remarks>
    public FakeUserDirectory Users { get; } = new FakeUserDirectory()
        .With(11, "T Raj", "tech@fujitec.co.in")
        .With(12, "L Menon", "lead@fujitec.co.in")
        .With(13, "B Rao", "branch@fujitec.co.in", capability: "request.manage", branchId: 2)
        .With(14, "M Nair", "manager@fujitec.co.in", employeeId: 600);

    public FakeEmployeeDirectory Employees { get; } = new FakeEmployeeDirectory()
        .Reports(500, 600);

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_SlaPolicy_Name", "SlaPolicy.NameTaken",
            "A policy with that name already exists.")
        .Register("UX_SlaPolicy_ActivePriority", "SlaPolicy.PriorityTaken",
            "Another active policy already covers that priority. Retire it first.")
        .Register("UX_SlaEscalation_PolicyTypeLevel", "SlaEscalation.LevelTaken",
            "That policy already has an escalation at this level.")
        .Register("UX_SlaEscalationLog_OncePerLevel", "SlaEscalation.AlreadyFired",
            "That escalation has already fired for this ticket.")
        .Register("UX_LocationOperationalHour_Location", "LocationCalendar.Exists",
            "That branch already has a calendar.")
        .Register("UX_LocationOperationalDay_Day", "LocationCalendar.DayTaken",
            "That weekday is already set for this branch.")
        .Register("UX_LocationSaturdayRule_Occurrence", "LocationCalendar.SaturdayTaken",
            "That Saturday occurrence is already set for this branch.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public ServiceLevelDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ServiceLevelDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", ServiceLevelDbContext.SchemaName))
            .Options);

    /// <summary>
    /// Empties everything. Children first, and the escalation log before the
    /// escalations it points at — that foreign key is NO ACTION, because an
    /// escalation that fired is evidence.
    /// </summary>
    public async Task ResetAsync()
    {
        Clock.Reset();
        Tickets.Reset();
        Notifier.Reset();

        await EmptyAsync();
    }

    private async Task EmptyAsync() => await ExecuteAsync("""
        DELETE FROM [ServiceLevel].[SlaEscalationLog];
        DELETE FROM [ServiceLevel].[SlaEscalation];
        DELETE FROM [ServiceLevel].[SlaPolicy];
        DELETE FROM [ServiceLevel].[HolidayLocation];
        DELETE FROM [ServiceLevel].[HolidayCalendar];
        DELETE FROM [ServiceLevel].[LocationSaturdayRule];
        DELETE FROM [ServiceLevel].[LocationOperationalDay];
        DELETE FROM [ServiceLevel].[LocationOperationalHour];
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

[CollectionDefinition(nameof(ServiceLevelCollectionDefinition))]
public sealed class ServiceLevelCollectionDefinition : ICollectionFixture<ServiceLevelFixture>;
