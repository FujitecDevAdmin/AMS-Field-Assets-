using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.Tests;

/// <summary>A clock the tests own, so nothing depends on the wall clock.</summary>
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

/// <summary>
/// A real Organization schema, built by the module's own migrations.
/// </summary>
/// <remarks>
/// Employee is system-versioned, so the tables cannot simply be emptied with
/// DELETE while versioning is on for every case — see <see cref="ResetAsync"/>.
/// </remarks>
public sealed class OrganizationFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_OrganizationTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>
    /// Every unique index in this schema, with the 409 it produces.
    /// <c>SqlErrorRegistrationTests</c> proves the list is complete against the
    /// live schema, so adding an index without adding a line here fails.
    /// </summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_Region_Name", "Region.NameTaken", "A region with that name already exists.")
        .Register("UX_Branch_Code", "Branch.CodeTaken", "A branch with that code already exists.")
        .Register("UX_Branch_OneHeadOffice", "Branch.HeadOfficeExists",
            "Another branch is already the head office. Clear that one first.")
        .Register("UX_Department_Name", "Department.NameTaken", "A department with that name already exists.")
        .Register("UX_Vendor_Name", "Vendor.NameTaken", "A vendor with that name already exists.")
        .Register("UX_Employee_Code", "Employee.CodeTaken", "An employee with that code already exists.")
        .Register("UX_Application_Name", "Application.NameTaken", "An application with that name already exists.")
        .Register("UX_EmployeeApplication_OneActive", "ApplicationAccess.AlreadyGranted",
            "That employee already has access to this application.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public OrganizationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", OrganizationDbContext.SchemaName))
            .Options);

    /// <summary>
    /// Empties every table so one test cannot see another's rows.
    /// </summary>
    /// <remarks>
    /// Employee is system-versioned and its history table refuses a plain
    /// DELETE of the versions, so versioning is switched off for the wipe and
    /// straight back on. Only a test database is ever treated this way.
    /// </remarks>
    public async Task ResetAsync()
    {
        await ExecuteAsync("DELETE FROM [Organization].[EmployeeApplication];");

        // Each of these is its own batch on purpose. SQL Server compiles a
        // batch as a unit, so a DELETE against the history table in the SAME
        // batch as the ALTER that switches versioning off is rejected before
        // the ALTER has run.
        await ExecuteAsync("ALTER TABLE [Organization].[Employee] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Organization].[Employee];");
        await ExecuteAsync("DELETE FROM [Organization].[EmployeeHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Organization].[Employee]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Organization].[EmployeeHistory]));
            """);

        await ExecuteAsync("""
            DELETE FROM [Organization].[Application];
            DELETE FROM [Organization].[Branch];
            DELETE FROM [Organization].[Region];
            DELETE FROM [Organization].[Department];
            DELETE FROM [Organization].[Vendor];
            """);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)value;
    }

    private static async Task DropDatabaseAsync() =>
        await ExecuteOnMasterAsync($"""
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

[CollectionDefinition(nameof(OrganizationCollectionDefinition))]
public sealed class OrganizationCollectionDefinition : ICollectionFixture<OrganizationFixture>;
